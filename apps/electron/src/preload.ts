import { contextBridge } from 'electron';

const electronAPI = {
  platform: 'electron',
} as const;

contextBridge.exposeInMainWorld('electronAPI', electronAPI);
