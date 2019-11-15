import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'indexer',
  templateUrl: './indexer.component.html'
})
export class IndexerComponent {
    public submitStartPackage(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    }
}

interface StartPackage {
  name: string;
  version: string;
  frameworkFilter: string;
}
