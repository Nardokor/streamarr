import React, { useState } from 'react';
import Icon from 'Components/Icon';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons } from 'Helpers/Props';
import { useYouTubeSettings } from 'Settings/YouTube/useYouTubeSettings';
import AddSourceModal from './AddSourceModal';
import EditSourceModal from './EditSourceModal';
import SourceCard from './SourceCard';
import styles from './Sources.css';

function AddSourceCard({ onPress }: { onPress: () => void }) {
  return (
    <div className={styles.addCard} onClick={onPress}>
      <Icon name={icons.ADD} size={28} />
    </div>
  );
}

function SourcesPage() {
  const [editingSource, setEditingSource] = useState<string | null>(null);
  const [addingSource, setAddingSource] = useState(false);
  const { data: ytSettings } = useYouTubeSettings();

  const isYouTubeConfigured = !!ytSettings?.youTubeApiKey;

  return (
    <PageContent title="Sources">
      <PageContentBody>
        <div className={styles.cards}>
          {isYouTubeConfigured && (
            <SourceCard
              name="YouTube"
              isConfigured={true}
              onPress={() => setEditingSource('youtube')}
            />
          )}

          <AddSourceCard onPress={() => setAddingSource(true)} />
        </div>
      </PageContentBody>

      <AddSourceModal
        isOpen={addingSource}
        configuredSources={isYouTubeConfigured ? ['youtube'] : []}
        onSelect={(source) => {
          setAddingSource(false);
          setEditingSource(source);
        }}
        onModalClose={() => setAddingSource(false)}
      />

      <EditSourceModal
        source={editingSource}
        isOpen={editingSource !== null}
        onModalClose={() => setEditingSource(null)}
      />
    </PageContent>
  );
}

export default SourcesPage;
