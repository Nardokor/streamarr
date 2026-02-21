import React, { useState } from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { useYouTubeSettings } from 'Settings/YouTube/useYouTubeSettings';
import EditSourceModal from './EditSourceModal';
import SourceCard from './SourceCard';
import styles from './Sources.css';

function SourcesPage() {
  const [editingSource, setEditingSource] = useState<string | null>(null);
  const { data: ytSettings } = useYouTubeSettings();

  return (
    <PageContent title="Sources">
      <PageContentBody>
        <div className={styles.cards}>
          <SourceCard
            name="YouTube"
            isConfigured={!!ytSettings?.apiKey}
            onPress={() => setEditingSource('youtube')}
          />
        </div>
      </PageContentBody>

      <EditSourceModal
        source={editingSource}
        isOpen={editingSource !== null}
        onModalClose={() => setEditingSource(null)}
      />
    </PageContent>
  );
}

export default SourcesPage;
