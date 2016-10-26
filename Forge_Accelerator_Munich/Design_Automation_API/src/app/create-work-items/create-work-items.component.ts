import {Component, OnInit, Input} from '@angular/core';
import {Observable} from "rxjs";
import {
  ForgeService,
  Bucket,
  BucketFile,
  Header,
  DerivativeManifest,
  WorkItem,
  WorkItemArguments,
  WorkItemArgument
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

  workItems: Observable<WorkItem[]>;
  taskCount: number = 3;

  xref_bucketFile: BucketFile;
  ngOnInit() {
    this.forgeService.getBucketFile('automation_api_tests', 'Drawing.zip').subscribe((bucketFile: BucketFile) => {
      this.xref_bucketFile = bucketFile;
    })
  }

  onTaskCountChanged(event){
    this.taskCount = event.target.value;
  }

  onSubmit(event) {
    event.preventDefault()
    this.createWorkItems(this.bucketFile);
  }

  createWorkItems(bucketFile: BucketFile) {
    console.log("OnSubmit", bucketFile);
    this.workItems = Observable.forkJoin(this.createObservables());
  }

  private createObservables() : Observable<WorkItem>[] {
    console.log("Fork Join...",this.taskCount);
    var observables = [];
    var states = ['InProgress', 'Succeeded', 'Failed']
      for (let i = 0; i < this.taskCount; ++i) {
        {
          observables.push(this.CreteWorkItemForXRefFile())
          observables.push(
            new Observable<WorkItem>(observer=> {
                var workItem = new WorkItem();
                workItem.Status = states[Math.floor(Math.random() * states.length)];
                workItem.ActivityId = "Activity";
                console.log("Observer running...",workItem);
                observer.next(workItem);
                /*  if (workItem.Status.startsWith("Failed"))
                 observer.error("Error");
                 if (workItem.Status == "Succeeded")*/
                // observer.complete();
              }
            ));
        }
            // .delay((Math.floor(Math.random() * ( 1 + 5000 - 1000 )) + 1000)));
      }
      return observables;
  }

  private CreteWorkItemForXRefFile(): Observable<WorkItem> {
    var workItem = new WorkItem();
    workItem.ActivityId = "PlotToPDF";
    workItem.Arguments = new WorkItemArguments();
    var inputFile = new WorkItemArgument();
    inputFile.Name = "HostDwg";
    inputFile.Resource = this.xref_bucketFile.location;
    var authorizationHeader = new Header();
    authorizationHeader.Name = "Authorization"
    authorizationHeader.Value ='Bearer eIKmx1enQimMZXYUsrOeFURW6wIT'
    inputFile.Headers = [authorizationHeader]
    inputFile.ResourceKind = "EtransmitPackage";
    workItem.Arguments.InputArguments = [inputFile];

    return this.forgeService.processWorkItem(this.xref_bucketFile.bucketKey, workItem, "autocad.io", 'Xrefs.pdf')
      .filter((workItem: WorkItem) => workItem.Status == "Succeeded");
  }
}
