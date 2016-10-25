import {Injectable} from '@angular/core';
import {Headers, Response, RequestOptions, Http, Jsonp, ResponseContentType} from '@angular/http';
import {Observable, Subject} from 'rxjs/Rx';

@Injectable()
export class ForgeService {
  basePath: string = 'https://developer.api.autodesk.com/';
  authPath: string = 'http://www.threadmodeler.com/assets/oauth/authenticate.php';

  constructor(private http: Http, private jsonp: Jsonp) {
  }

  getActivities() {
    return this.getAuthorizationHeader([Scope.CodeAll])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers});
        return this.http.get(this.basePath + 'autocad.io/us-east/v2/Activities', options)
          .map((res: Response) => res.json());
      });
  }

  getBucket(bucketKey: string): Observable<Bucket> {
    return this.getAuthorizationHeader([Scope.BucketRead])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers});
        return this.http.get(this.basePath + 'oss/v2/buckets/' + bucketKey + '/details', options)
          .map((res: Response) => res.json());
      });
  }

  uploadFileToBucket(bucketKey: string, file: File, onProgress): Observable<BucketFile> {
    return this.getAuthorizationHeader([Scope.DataWrite])
      .flatMap(headers => {
        return new Observable<BucketFile>(observer => {

          var xhr = new XMLHttpRequest();

          xhr.onreadystatechange = function () {
            if (xhr.readyState === 4 && (xhr.status === 200 || xhr.status === 201)) {
              var uploadedFileObject = JSON.parse(xhr.response);
              observer.next(uploadedFileObject);
              observer.complete();
            }
          };
          xhr.upload.onprogress = onProgress;
          xhr.open("PUT", this.basePath + 'oss/v2/buckets/' + bucketKey + '/objects/' + file.name, true);
          xhr.withCredentials = true;
          xhr.setRequestHeader('Content-Type', 'application/octet-stream');
          xhr.setRequestHeader('Authorization', headers.get('Authorization'));
          xhr.send(file);
        });
      });
  }

  getBucketFiles(bucketKey: string, limit: number = 10, beginsWith: string = null): Observable<BucketFile> {
    return this.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers});
        var url = this.basePath + 'oss/v2/buckets/' + bucketKey + "/objects?limit=" + limit;
        if (beginsWith != null)
          url += "&beginsWith=" + beginsWith;
        return this.http.get(url, options)
          .map((res: Response) => res.json().items)
          .flatMap(items => items)
          .map(item => <BucketFile> item);
      });
  }

  getBucketFile(bucketKey: string, fileName: string): Observable<BucketFile> {
    return this.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers});
        return this.http.get(this.basePath + 'oss/v2/buckets/' + bucketKey + "/objects/" + fileName + "/details", options)
          .map((res: Response) => res.json());
      });
  }

  downloadBucketFile(bucketFile: BucketFile) {
    return this.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers, responseType: ResponseContentType.ArrayBuffer});
        return this.http.get(this.basePath + 'oss/v2/buckets/' + bucketFile.bucketKey + "/objects/" + bucketFile.objectKey, options);
      });
  }

  processWorkItem(bucketKey: string, workItem: WorkItem, engine: string, resultFileName: string): Observable<WorkItem> {
    return this.getAuthorizationHeader([Scope.CodeAll, Scope.DataRead, Scope.DataWrite, Scope.DataCreate])
      .flatMap(headers => {
        headers.append("Content-Type", "application/json");
        let options = new RequestOptions({headers: headers});

        var outputFile = new WorkItemArgument();
        outputFile.Name = "Result";
        outputFile.HttpVerb = "PUT";
        outputFile.Resource = this.basePath + 'oss/v2/buckets/' + bucketKey + '/objects/' + resultFileName;
        var authorizationHeader = new Header();
        authorizationHeader.Name = "Authorization"
        authorizationHeader.Value = headers.get('Authorization')
        outputFile.Headers = [authorizationHeader]
        workItem.Arguments.OutputArguments = [outputFile];
        return this.http.post(this.basePath + engine + "/us-east/v2/WorkItems", JSON.stringify(workItem), options)
          .map((res: Response) => [headers, res.json()]);
      })
      .flatMap(result => {
        return new Observable(observer => {
          var stop = new Subject();
          Observable
            .interval(1000)
            .takeUntil(stop)
            .flatMap(()=> this.http.get(this.basePath + engine + "('" + result[1].Id + "')", new RequestOptions({headers: result[0]}))
              .map((res: Response) => res.json()))
            .subscribe(workItem => {
              observer.next(workItem);
              if (workItem.Status == "Succeeded") {
                stop.next(true);
                observer.complete();
              }
              else if (workItem.Status.startsWith("Failed")) {
                stop.next(true);
                observer.error('Processing file failed!');
              }
            })
        });
      })
  }

  translateDerivative(uploadedFileUrnBase64, format = 'svf') {
    return this.getAuthorizationHeader([Scope.DataRead, Scope.DataWrite])
      .flatMap(headers => {
        headers.append("Content-Type", "application/json");
        headers.append("x-ads-force", 'true');
        let options = new RequestOptions({headers: headers});
        let body = "{\n\"input\": {\n \"urn\": \"" + uploadedFileUrnBase64 + "\"\n },\n\"output\": {\n \"formats\": [\n{\n\"type\": \"" + format + "\",\n \"views\": [\n \"2d\",\n \"3d\"]}]}}";
        return this.http.post(this.basePath + 'modelderivative/v2/designdata/job', body, options)
          .map((res: Response) => res.json());
      })
      .flatMap(job => {
        return new Observable(observer => {
          var stop = new Subject();
          Observable
            .interval(1000)
            .takeUntil(stop)
            .flatMap(()=> this.getDerivativeManifest(uploadedFileUrnBase64))
            .subscribe(manifest => {
              observer.next(manifest);
              if (manifest.status == "success" && manifest.progress == "complete" && manifest.derivatives.length > 0) {
                stop.next(true);
                observer.complete();
              }
              else if (manifest.status == "failed" || manifest.status == "timeout") {
                stop.next(true);
                observer.error('Translating file failed!');
              }
            })
        });
      })
  }

  getDerivativeManifest(uploadedFileUrnBase64): Observable<DerivativeManifest> {
    return this.getAuthorizationHeader([Scope.DataRead])
      .flatMap(headers => {
        let options = new RequestOptions({headers: headers});
        return this.http.get(this.basePath + '/modelderivative/v2/designdata/' + uploadedFileUrnBase64 + "/manifest", options)
          .map((res: Response) => res.json());
      });
  }

  getAuthorizationHeader(scopes: string[]): Observable<Headers> {
    return this.getAccessToken(scopes)
      .map(token => {
        let headers = new Headers();
        headers.append('Authorization', token.token_type + ' ' + token.access_token);
        return headers;
      });
  }

  getAccessToken(scopes: string[]): Observable<AccessToken> {
    return this.jsonp.get(this.authPath + "?callback=JSONP_CALLBACK&scope[]=" + scopes.join('&scope[]='))
      .map((response: any) => response.json());
  }
}

export class WorkItem {
  ActivityId: string;
  Arguments: WorkItemArguments;
  Status: string;
  Id: string;
}

export class WorkItemArguments {
  InputArguments: WorkItemArgument[];
  OutputArguments: WorkItemArgument[];
}

export class WorkItemArgument {
  Resource: string;
  Name: string;
  Headers: Header[];
  ResourceKind: string;
  StorageProvider: string;
  HttpVerb: string;
}

export class Header {
  Name: string;
  Value: string;
}


export class DerivativeManifest {
  urn: string;
  type: string;
  progress: string;
  status: string;
  hasThumbnail: boolean;
  derivatives: Derivative[];
}

export class Derivative {
  name: string;
  outputType: string;
  progress: string;
  status: string;
  hasThumbnail: boolean;
  children: DerivativeChild[];
}

export class DerivativeChild {
  urn: string;
  mime: string;
  role: string;
  progress: string;
  status: string;
  children: DerivativeChild[];
}


export class Scope {
  public static readonly UserRead = "user-profile:read";
  public static readonly AccountRead = "account:read";
  public static readonly AccountWrite = "account:write";
  public static readonly DataRead = "data:read";
  public static readonly DataWrite = "data:write";
  public static readonly DataCreate = "data:create";
  public static readonly DataSearch = "data:search";
  public static readonly BucketCreate = "bucket:create";
  public static readonly BucketRead = "bucket:read";
  public static readonly BucketUpdate = "bucket:update";
  public static readonly BucketDelete = "bucket:delete";
  public static readonly CodeAll = "code:all";
}

export class AccessToken {
  token_type: string;
  access_token: string;
  expires_in: number;
}

export class Bucket {
  bucketKey: string;
  bucketOwner: string;
  createdDate: string;
  permissions;
  policyKey: string;
}

export class BucketFile {
  bucketKey: string;
  objectId: string;
  objectKey: string;
  sha1: string;
  size: number;
  "content-type": string;
  location: string;
}
