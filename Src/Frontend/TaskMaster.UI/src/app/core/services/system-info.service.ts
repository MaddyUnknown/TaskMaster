import { Injectable } from '@angular/core';
import { SystemInfo } from '../models';

@Injectable({ providedIn: 'root' })
export class SystemInfoService {
  private info: SystemInfo = { version: '—', environment: '—' };

  get current(): SystemInfo {
    return this.info;
  }

  load(systemInfo: SystemInfo): void {
    this.info = { ...this.info, ...systemInfo };
  }
}