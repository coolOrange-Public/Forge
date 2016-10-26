import {Component, OnInit, Input} from '@angular/core';
import {BucketFile, ForgeService, WorkItem} from "../shared/forge.service";
import {Observable} from "rxjs";

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

  ngOnInit() {
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
                observer.complete();
            }
          ));
            // .delay((Math.floor(Math.random() * ( 1 + 5000 - 1000 )) + 1000)));
      }
      return observables;
  }
}
