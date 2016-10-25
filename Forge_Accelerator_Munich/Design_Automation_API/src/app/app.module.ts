import './rxjs-extensions';
import {BrowserModule} from "@angular/platform-browser";
import {NgModule} from "@angular/core";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {HttpModule, JsonpModule} from "@angular/http";
import {ForgeService} from './shared/forge.service';

import {AppComponent} from "./app.component";
import { FileuploadComponent } from './fileupload/fileupload.component';
import { CreateWorkItemsComponent } from './create-work-items/create-work-items.component';

@NgModule({
    declarations: [
        AppComponent,
        FileuploadComponent,
        CreateWorkItemsComponent
    ],
    imports: [
        BrowserModule,
        FormsModule,
        ReactiveFormsModule,
        HttpModule,
        JsonpModule
    ],
    providers: [
      ForgeService
    ],
    bootstrap: [AppComponent]
})
export class AppModule {
}
