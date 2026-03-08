import React, { useCallback, useEffect, useState } from 'react';
import { useHistory } from 'react-router';
import Alert from 'Components/Alert';
import TextInput from 'Components/Form/TextInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import useDebounce from 'Helpers/Hooks/useDebounce';
import { icons, kinds } from 'Helpers/Props';
import { CreatorLookupResult } from 'typings/Creator';
import { InputChanged } from 'typings/inputs';
import AddCreatorModal from './AddCreatorModal';
import useCreators, { useCreatorLookup } from './useCreators';
import styles from './CreatorAdd.css';

interface SearchResultCardProps {
  result: CreatorLookupResult;
  onPress: () => void;
}

function SearchResultCard({ result, onPress }: SearchResultCardProps) {
  const isExisting = result.existingCreatorId != null;

  const linkProps = isExisting
    ? { to: `/creator/${result.existingCreatorId}` }
    : { onPress };

  return (
    <div className={styles.searchResult}>
      <Link className={styles.underlay} {...linkProps} />

      <div className={styles.overlay}>
        {result.thumbnailUrl ? (
          <img
            className={styles.thumbnail}
            src={result.thumbnailUrl}
            alt={result.name}
          />
        ) : null}

        <div className={styles.content}>
          <div className={styles.titleRow}>
            <div className={styles.title}>{result.name}</div>

            <div className={styles.icons}>
              {isExisting ? (
                <Icon
                  className={styles.alreadyExistsIcon}
                  name={icons.CHECK_CIRCLE}
                  size={36}
                  title="Already in your library"
                />
              ) : null}
            </div>
          </div>

          {result.channels.length > 0 ? (
            <div className={styles.channels}>
              {result.channels.map((ch) => (
                <span key={ch.platformId} className={styles.channelTag}>
                  {ch.platform}: {ch.title}
                </span>
              ))}
            </div>
          ) : null}

          {result.description ? (
            <div className={styles.overview}>{result.description}</div>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function CreatorAdd() {
  const history = useHistory();
  const [term, setTerm] = useState('');
  const [isFetching, setIsFetching] = useState(false);
  const [selected, setSelected] = useState<CreatorLookupResult | null>(null);
  const query = useDebounce(term, term ? 300 : 0);

  const { data: creators } = useCreators();
  const hasCreators = (creators ?? []).length > 0;

  const {
    isFetching: isFetchingApi,
    error,
    data,
  } = useCreatorLookup(query);

  useEffect(() => {
    setIsFetching(isFetchingApi);
  }, [isFetchingApi]);

  const handleSearchChange = useCallback(({ value }: InputChanged<string>) => {
    setTerm(value);
    setIsFetching(!!value.trim());
  }, []);

  const handleClearPress = useCallback(() => {
    setTerm('');
    setIsFetching(false);
  }, []);

  const results = data ? [data] : [];

  return (
    <PageContent title="Add New Creator">
      <PageContentBody>
        <div className={styles.searchContainer}>
          <div className={styles.searchIconContainer}>
            <Icon name={icons.SEARCH} size={20} />
          </div>

          <TextInput
            className={styles.searchInput}
            name="creatorLookup"
            value={term}
            placeholder="eg. MrBeast, https://www.youtube.com/@MrBeast"
            autoFocus={true}
            onChange={handleSearchChange}
          />

          <Button
            className={styles.clearLookupButton}
            onPress={handleClearPress}
          >
            <Icon name={icons.REMOVE} size={20} />
          </Button>
        </div>

        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <div className={styles.message}>
            <div className={styles.helpText}>Creator not found</div>

            <Alert kind={kinds.DANGER}>
              No creator was found for that name or URL. Try a YouTube channel
              URL like https://www.youtube.com/@ChannelName
            </Alert>
          </div>
        ) : null}

        {!isFetching && !error && results.length > 0 ? (
          <div className={styles.searchResults}>
            {results.map((result) => (
              <SearchResultCard
                key={result.channels[0]?.platformId ?? result.name}
                result={result}
                onPress={() => setSelected(result)}
              />
            ))}
          </div>
        ) : null}

        {!isFetching && !error && results.length === 0 && term ? (
          <div className={styles.message}>
            <div className={styles.noResults}>No results found for "{term}"</div>
          </div>
        ) : null}

        {!term ? (
          <div className={styles.message}>
            <div className={styles.helpText}>
              Start typing to search for a creator by name, channel handle, or URL
            </div>
          </div>
        ) : null}

        {!term && !hasCreators ? (
          <div className={styles.message}>
            <div className={styles.noCreatorsText}>
              No creators have been added yet
            </div>

            <div>
              <Button to="/add/import" kind={kinds.PRIMARY}>
                Import Existing Library
              </Button>
            </div>
          </div>
        ) : null}
      </PageContentBody>

      {selected ? (
        <AddCreatorModal
          isOpen={true}
          creator={selected}
          onModalClose={() => setSelected(null)}
          onCreatorAdded={() => history.push('/creator')}
        />
      ) : null}
    </PageContent>
  );
}

export default CreatorAdd;
