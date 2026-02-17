import { HTML5toTouch } from 'rdndmb-html5-to-touch';
import React from 'react';
import { DndProvider } from 'react-dnd-multi-backend';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbar from 'Settings/SettingsToolbar';
import translate from 'Utilities/String/translate';
import QualityProfiles from './Quality/QualityProfiles';

// Only a single DragDrop Context can exist so it's done here to allow editing
// quality profiles to work.

function Profiles() {
  return (
    <PageContent title={translate('Profiles')}>
      <SettingsToolbar showSave={false} />

      <PageContentBody>
        <DndProvider options={HTML5toTouch}>
          <QualityProfiles />
        </DndProvider>
      </PageContentBody>
    </PageContent>
  );
}

export default Profiles;
