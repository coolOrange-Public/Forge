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
        console.log(activities);
        this.activities = activities;
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
      this.CreateWorkItem(this.workItems[i].ActivityId)
        .subscribe((workItem: WorkItem) => {
          this.workItems[i] = workItem;
        }, error=> {
          this.workItems[i].Status = error;
        });
    }
  }

  private CreateWorkItem(activityId: string): Observable<WorkItem> {
    return this.forgeService.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        var workItem = new WorkItem();
        workItem.ActivityId = activityId;
        workItem.Arguments = new WorkItemArguments();
        var inputFile = new WorkItemArgument();
        inputFile.Name = "HostDwg";
        inputFile.Resource = this.bucketFile.location;
        var authorizationHeader = new Header();
        authorizationHeader.Name = "Authorization"
        authorizationHeader.Value = headers.get('Authorization');
        inputFile.Headers = [authorizationHeader]
        workItem.Arguments.InputArguments = [inputFile];
        return this.forgeService.processWorkItem(this.bucketFile.bucketKey, workItem, "autocad.io", activityId + '_' + this.bucketFile.objectKey);
      });
  }

  private CreteWorkItemForXRefFile(): Observable<WorkItem> {
    var workItem = new WorkItem();
    workItem.ActivityId = "PlotToPDF";
    workItem.Arguments = new WorkItemArguments();
    var inputFile = new WorkItemArgument();
    inputFile.Name = "HostDwg";
    inputFile.Resource = this.bucketFile.location;
    var authorizationHeader = new Header();
    authorizationHeader.Name = "Authorization"
    authorizationHeader.Value = 'Bearer eIKmx1enQimMZXYUsrOeFURW6wIT'
    inputFile.Headers = [authorizationHeader]
    inputFile.ResourceKind = "EtransmitPackage";
    workItem.Arguments.InputArguments = [inputFile];

    return this.forgeService.processWorkItem(this.bucketFile.bucketKey, workItem, "autocad.io", 'Xrefs.pdf');
  }
}
