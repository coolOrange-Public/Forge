import {Component, OnInit, Input} from '@angular/core';
import {Observable,Subject} from "rxjs";
import {
  ForgeService,
  Bucket,
  BucketFile,
  Header,
  Scope,
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

  bucketFiles_with_references: BucketFile[] = [];

  authorizationHeader : Header;
  ngOnInit() {
    this.forgeService.getAuthorizationHeader([Scope.CodeAll, Scope.DataRead, Scope.DataWrite, Scope.DataCreate]).subscribe(headers => {
          this.authorizationHeader = new Header();
          this.authorizationHeader.Name = "Authorization"
          this.authorizationHeader.Value = headers.get('Authorization')
      });
    this.forgeService.getBucketFile('automation_api_tests', 'Drawing.zip').subscribe((bucketFile: BucketFile) => {
      this.xref_bucketFile = bucketFile;
    })
    this.forgeService.getBucketFile('automation_api_tests', 'Drawing3.dwg').last().subscribe((bucketFile: BucketFile) => {
      this.bucketFiles_with_references.push(bucketFile);
    })
    this.forgeService.getBucketFile('automation_api_tests', 'Drawing4.dwg').last().subscribe((bucketFile: BucketFile) => {
      this.bucketFiles_with_references.push(bucketFile);
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
    this.workItems.subscribe(w=>{
      console.log(w)
    })
  }

  private createObservables() : Observable<WorkItem>[] {
    console.log("Fork Join...",this.taskCount);
    var observables = [];
    var states = ['InProgress', 'Succeeded', 'Failed']
      for (let i = 0; i < this.taskCount; ++i) {
        {
          observables.push(this.CreteWorkItemFor_XRefFile())
          observables.push(this.CreteWorkItemsFor_FilesWithMissingReferences())
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

  private CreteWorkItemFor_XRefFile(): Observable<WorkItem> {
    var workItem = new WorkItem();
    workItem.ActivityId = "PlotToPDF";
    workItem.Arguments = new WorkItemArguments();
    var inputFile = new WorkItemArgument();
    inputFile.Name = "HostDwg";
    inputFile.Resource = this.xref_bucketFile.location;
    inputFile.Headers = [this.authorizationHeader]
    inputFile.ResourceKind = "EtransmitPackage";
    workItem.Arguments.InputArguments = [inputFile];
    return this.forgeService.processWorkItem(this.xref_bucketFile.bucketKey, workItem, "autocad.io", 'Xrefs.pdf')
      .filter((workItem: WorkItem) => workItem.Status == "Succeeded");
  }

  private CreteWorkItemsFor_FilesWithMissingReferences(): Observable<WorkItem> {
    return Observable.from(this.bucketFiles_with_references).flatMap((bucketFiles_with_reference:BucketFile) => {
      var workItem = new WorkItem();
      workItem.ActivityId = "PlotToPDF";
      workItem.Arguments = new WorkItemArguments();
      var inputFile = new WorkItemArgument();
      inputFile.Name = "HostDwg";
      inputFile.Resource = bucketFiles_with_reference.location;
      inputFile.Headers = [this.authorizationHeader]
      workItem.Arguments.InputArguments = [inputFile]
      return this.forgeService.processWorkItem(bucketFiles_with_reference.bucketKey, workItem, "autocad.io", 'MissingReferences.pdf')
        .filter((w: WorkItem) => w.Status == "Succeeded" || w.Status.startsWith("Failed")).last();
    })
  }
}
