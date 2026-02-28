import Provider from './Provider';

interface NotificationResource extends Provider {
  enable: boolean;
  onDownload: boolean;
  supportsOnDownload: boolean;
  presets?: NotificationResource[];
}

export default NotificationResource;
