import React, { useState } from 'react';
import Icon from 'Components/Icon';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons } from 'Helpers/Props';
import AddSourceModal from './AddSourceModal';
import EditSourceModal from './EditSourceModal';
import SourceCard from './SourceCard';
import { MetadataSourceResource, useMetadataSources } from './useMetadataSources';
import styles from './Sources.css';

function AddSourceCard({ onPress }: { onPress: () => void }) {
  return (
    <div className={styles.addCard} onClick={onPress}>
      <Icon name={icons.ADD} size={28} />
    </div>
  );
}

function SourcesPage() {
  const [editingSource, setEditingSource] =
    useState<MetadataSourceResource | null>(null);
  const [addingSource, setAddingSource] = useState(false);

  const { data: sources } = useMetadataSources();
  const configuredImplementations = (sources ?? []).map((s) => s.implementation);

  return (
    <PageContent title="Sources">
      <PageContentBody>
        <div className={styles.cards}>
          {(sources ?? []).map((source) => (
            <SourceCard
              key={source.id}
              name={source.name}
              isConfigured={source.enable}
              onPress={() => setEditingSource(source)}
            />
          ))}

          <AddSourceCard onPress={() => setAddingSource(true)} />
        </div>
      </PageContentBody>

      <AddSourceModal
        isOpen={addingSource}
        configuredImplementations={configuredImplementations}
        onSelect={(template) => {
          setEditingSource({ ...template, enable: true });
          setAddingSource(false);
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
