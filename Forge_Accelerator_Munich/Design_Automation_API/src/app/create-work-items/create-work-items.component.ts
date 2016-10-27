import {Component, OnInit, Input} from '@angular/core';
import {Observable} from "rxjs";
import {
  ForgeService,
  BucketFile,
  Header,
  WorkItem,
  WorkItemArguments,
  WorkItemArgument, Scope, Activity
} from '../shared/forge.service';
import {forEach} from "@angular/router/src/utils/collection";

@Component({
  selector: 'app-create-work-items',
  templateUrl: './create-work-items.component.html',
  styleUrls: ['./create-work-items.component.css']
})
export class CreateWorkItemsComponent implements OnInit {

  constructor(public forgeService: ForgeService) {
  }

  @Input()
  bucketFile: BucketFile;

  workItems: WorkItem[];
  taskCount: number = 3;
  activities: Activity[] = [];

  ngOnInit() {
    this.forgeService.getActivities()
      .subscribe((activities: Activity[]) => {
          this.activities = activities;
          var rndActivity = new Activity();
          rndActivity.Id = "Random";
          this.activities.push(rndActivity);
      })
  }

  onTaskCountChanged(event) {
    this.taskCount = event.target.value;
  }

  onSubmit(event) {
    event.preventDefault();
    this.createWorkItems(this.bucketFile);
  }

  createWorkItems(bucketFile: BucketFile) {
    this.workItems = [];
    var j = 0;

    for (let i = 0; i < this.taskCount; ++i) {
      if (j == this.activities.length)
        j = 0;
      var workItem = new WorkItem();
      workItem.Status = 'InProgress';
      workItem.ActivityId = this.activities[j].Id;
      this.workItems.push(workItem);
      j++;
    }

    for (let i = 0; i < this.workItems.length; ++i) {
      this.processWorkItem(this.workItems[i].ActivityId, bucketFile)
        .subscribe((workItem: WorkItem) => {
          this.workItems[i] = workItem;
        }, error=> {
          this.workItems[i].Status = error;
        });
    }
  }

  private processWorkItem(activityId: string, bucketFile: BucketFile): Observable<WorkItem> {
    return this.forgeService.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        var workItem = new WorkItem();
        workItem.ActivityId = activityId;
        workItem.Arguments = new WorkItemArguments();
        var inputFile = new WorkItemArgument();
        inputFile.Name = "HostDwg";
        inputFile.Resource = bucketFile.location;
        var authorizationHeader = new Header();
        authorizationHeader.Name = "Authorization"
        authorizationHeader.Value = headers.get('Authorization');
        inputFile.Headers = [authorizationHeader]
        workItem.Arguments.InputArguments = [inputFile];

        if (bucketFile.objectKey.endsWith(".zip"))
          return this.processWorkItemForXRefFile(workItem);
        return this.processSimpleWorkItem(activityId, bucketFile, workItem);
      });
  }

  private processSimpleWorkItem(activityId, bucketFile, workItem: WorkItem): Observable<WorkItem> {
    return this.forgeService.processWorkItem(bucketFile.bucketKey, workItem, "autocad.io", activityId + '_' + bucketFile.objectKey);
  }

  private processWorkItemForXRefFile(workItem: WorkItem): Observable<WorkItem> {
    workItem.Arguments.InputArguments[0].ResourceKind = "EtransmitPackage";
    return this.forgeService.processWorkItem(this.bucketFile.bucketKey, workItem, "autocad.io", 'Xrefs.pdf');
  }
}
