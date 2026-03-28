import React from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';

function BaseSettingsFields({
  getVal,
  onChange,
  showVideos,
  showShorts,
  showVods = true,
  showLive = true,
  showFilters = true,
  videosLabel = 'Videos',
  shortsLabel = 'Shorts',
}: {
  getVal: <T>(name: string, fallback: T) => T;
  onChange: (change: InputChanged) => void;
  showVideos: boolean;
  showShorts: boolean;
  showVods?: boolean;
  showLive?: boolean;
  showFilters?: boolean;
  videosLabel?: string;
  shortsLabel?: string;
}) {
  return (
    <>
      <FormGroup>
        <FormLabel>Full Refresh Interval (hours)</FormLabel>
        <FormInputGroup
          type={inputTypes.NUMBER}
          name="refreshIntervalHours"
          helpText="How often to scan for new content (min 1, max 168)"
          min={1}
          max={168}
          value={getVal('refreshIntervalHours', 1)}
          errors={[]}
          warnings={
            getVal('refreshIntervalHours', 1) > 6
              ? [{ message: 'Values above 6 hours may cause recent uploads and live streams to be missed' }]
              : []
          }
          onChange={onChange}
        />
      </FormGroup>

      {showVideos && (
        <FormGroup>
          <FormLabel>Default: Download {videosLabel}</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadVideos"
            helpText={`Include ${videosLabel.toLowerCase()} by default for new channels`}
            value={getVal('defaultDownloadVideos', true)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showShorts && (
        <FormGroup>
          <FormLabel>Default: Download {shortsLabel}</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadShorts"
            helpText={`Include ${shortsLabel.toLowerCase()} by default for new channels`}
            value={getVal('defaultDownloadShorts', true)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showVods && (
        <FormGroup>
          <FormLabel>Default: Download VoDs</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadVods"
            helpText="Include past livestreams by default for new channels"
            value={getVal('defaultDownloadVods', true)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showLive && (
        <FormGroup>
          <FormLabel>Default: Download Live</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadLive"
            helpText="Record active livestreams by default for new channels"
            value={getVal('defaultDownloadLive', false)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showFilters && (
        <>
          <FormGroup>
            <FormLabel>Default: Watched Words</FormLabel>
            <FormInputGroup
              type={inputTypes.TEXT}
              name="defaultWatchedWords"
              helpText="word1, word2 … — only matching content is wanted (blank = all)"
              value={getVal('defaultWatchedWords', '')}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>Default: Ignored Words</FormLabel>
            <FormInputGroup
              type={inputTypes.TEXT}
              name="defaultIgnoredWords"
              helpText="word1, word2 … — matching content is unwanted (blank = none)"
              value={getVal('defaultIgnoredWords', '')}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>Default: Watched Defeats Ignored</FormLabel>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="defaultWatchedDefeatsIgnored"
              helpText="Watched words take priority over ignored words"
              value={getVal('defaultWatchedDefeatsIgnored', true)}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>Default: Auto Download</FormLabel>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="defaultAutoDownload"
              helpText="Automatically queue missing content for download"
              value={getVal('defaultAutoDownload', true)}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>
        </>
      )}
    </>
  );
}

export default BaseSettingsFields;
