import React from 'react';
import Link from 'Components/Link/Link';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import translate from 'Utilities/String/translate';
import SettingsToolbar from './SettingsToolbar';
import styles from './Settings.css';

function Settings() {
  return (
    <PageContent title={translate('Settings')}>
      <SettingsToolbar hasPendingChanges={false} />

      <PageContentBody>
        <Link className={styles.link} to="/settings/mediamanagement">
          {translate('MediaManagement')}
        </Link>

        <div className={styles.summary}>
          {translate('MediaManagementSettingsSummary')}
        </div>

        <Link className={styles.link} to="/settings/profiles">
          {translate('Profiles')}
        </Link>

        <div className={styles.summary}>
          {translate('ProfilesSettingsSummary')}
        </div>

        <Link className={styles.link} to="/settings/quality">
          {translate('Quality')}
        </Link>

        <div className={styles.summary}>
          {translate('QualitySettingsSummary')}
        </div>

        <Link className={styles.link} to="/settings/tags">
          {translate('Tags')}
        </Link>

        <div className={styles.summary}>{translate('TagsSettingsSummary')}</div>

        <Link className={styles.link} to="/settings/general">
          {translate('General')}
        </Link>

        <div className={styles.summary}>
          {translate('GeneralSettingsSummary')}
        </div>

        <Link className={styles.link} to="/settings/ui">
          {translate('Ui')}
        </Link>

        <div className={styles.summary}>{translate('UiSettingsSummary')}</div>

        <Link className={styles.link} to="/settings/sources">
          Sources
        </Link>

        <div className={styles.summary}>
          Configure platform integrations for content syncing.
        </div>

        <Link className={styles.link} to="/settings/archival">
          Archival
        </Link>

        <div className={styles.summary}>
          Configure priority keywords and default retention policies for downloaded content.
        </div>

        <Link className={styles.link} to="/settings/downloadclient">
          Download Client
        </Link>

        <div className={styles.summary}>
          Configure yt-dlp path, format, concurrency, and other download settings.
        </div>

        <Link className={styles.link} to="/settings/connect">
          Connect
        </Link>

        <div className={styles.summary}>
          Configure notifications for Discord, Telegram, Plex, and more.
        </div>
      </PageContentBody>
    </PageContent>
  );
}

export default Settings;
