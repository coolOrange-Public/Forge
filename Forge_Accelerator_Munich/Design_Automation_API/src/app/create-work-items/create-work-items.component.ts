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
  workItems: Observable<WorkItem>[];
  taskCount: number;

  ngOnInit() {
  }

  onSubmit(event) {
    event.preventDefault()
    this.createWorkItems(this.bucketFile);
  }

  createWorkItems(bucketFile: BucketFile) {
   /* for (let i = 0; i < this.taskCount; ++i) {
      this.workItems.push(
        Observable.create(observer => {
          this.forgeService.processWorkItem()
            .subscribe()
      }));
    }*/
  }
}
