import {Component, NgZone, EventEmitter, OnInit} from "@angular/core";
import {ForgeService, Bucket, BucketFile} from '../shared/forge.service';
import {Input, Output} from "@angular/core/src/metadata/directives";

@Component({
  selector: 'app-fileupload',
  templateUrl: 'fileupload.component.html',
  styleUrls: ['fileupload.component.css']
})

export class FileuploadComponent implements OnInit {
  @Input()
  bucketKey: string;

  @Input()
  allowedExtension;

  @Output()
  onUpload = new EventEmitter();

  progress: number = 0;
  uploadResult;

  constructor(public forgeService: ForgeService, private ngZone: NgZone) {
  }

  ngOnInit() {
    this.progress = 0;
    this.uploadResult = {
      success: false,
      data: null
    };
  }

  upload(fileInfo: File) {
    this.ngOnInit();

    this.forgeService.getBucket(this.bucketKey)
      .subscribe((bucket: Bucket) => {
        this.progress = 10;
        this.forgeService.uploadFileToBucket(bucket.bucketKey, fileInfo, this.onProgress)
          .subscribe((bucketFile: BucketFile) => {
            this.progress = 100;
            this.uploadResult.success = true;
            this.uploadResult.data = bucketFile;
            this.onUpload.emit(bucketFile);
          }, error => {
            this.uploadResult.data = error;
          });
      }, error => {
        this.uploadResult.data = error;
      });
  }

  onProgress = (progressEvent: ProgressEvent): void => {
    this.ngZone.run(() => {
      if (progressEvent.lengthComputable) {
        this.progress = 10 + Math.round((progressEvent.loaded / progressEvent.total) * 89);
      }
    });
  }

  inputFile: File;

  onFileInputChanged(event) {
    this.inputFile = event.srcElement.files[0];
  }

  onSubmit(event) {
    event.preventDefault()
    this.upload(this.inputFile);
  }


  dropUploadZone(event) {
    event.preventDefault();
    event.srcElement.className = 'upload-drop-zone drop';
    var file = event.dataTransfer.files[0];
    if (!file.name.endsWith(this.allowedExtension))
      alert("Only files with extension " + this.allowedExtension + " can be uploaded!");
    else {
      this.inputFile = file;
      this.upload(this.inputFile);
    }
  }

  dragoverUploadZone(event) {
    event.srcElement.className = 'upload-drop-zone drop';
    return false;
  }

  dragleaveUploadZone(event) {
    event.srcElement.className = 'upload-drop-zone';
    return false;
  }
}
