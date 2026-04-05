import React, { useRef } from 'react';
import Alert from 'Components/Alert';
import SpinnerButton from 'Components/Link/SpinnerButton';
import { kinds } from 'Helpers/Props';
import {
  useCookieStatus,
  useDeleteCookies,
  useUploadCookies,
} from 'Settings/Sources/useMetadataSources';

interface CookieUploadSectionProps {
  // Undefined when the source hasn't been saved yet. In this case the
  // component buffers the selected file locally and reports it via
  // onPendingFileChange so the parent can upload it after save.
  sourceId?: number;
  pendingFile?: File | null;
  onPendingFileChange?: (file: File | null) => void;
}

function CookieUploadSection({
  sourceId,
  pendingFile,
  onPendingFileChange,
}: CookieUploadSectionProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Existing-source mode: query real status from the API.
  const { data: status } = useCookieStatus(sourceId ?? 0, { enabled: sourceId != null });
  const { mutate: upload, isPending: isUploading, error: uploadError } = useUploadCookies(sourceId ?? 0);
  const { mutate: remove, isPending: isRemoving } = useDeleteCookies(sourceId ?? 0);

  const isNew = sourceId == null;
  const hasCookies = isNew ? pendingFile != null : (status?.hasCookies ?? false);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;

    if (isNew) {
      onPendingFileChange?.(file);
    } else if (file) {
      upload(file);
    }

    e.target.value = '';
  };

  const handleRemove = () => {
    if (isNew) {
      onPendingFileChange?.(null);
    } else {
      remove();
    }
  };

  return (
    <div style={{ marginTop: '16px', borderTop: '1px solid var(--borderColor)', paddingTop: '16px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
        <strong style={{ minWidth: '120px' }}>Cookies</strong>

        {hasCookies ? (
          <>
            <span style={{ color: 'var(--successColor, green)' }}>
              {isNew ? pendingFile?.name : 'Cookie file uploaded'}
            </span>

            <SpinnerButton
              kind={kinds.DEFAULT}
              isSpinning={!isNew && isUploading}
              onPress={() => fileInputRef.current?.click()}
            >
              Replace
            </SpinnerButton>

            <SpinnerButton
              kind={kinds.DANGER}
              isSpinning={!isNew && isRemoving}
              onPress={handleRemove}
            >
              Remove
            </SpinnerButton>
          </>
        ) : (
          <>
            <span style={{ color: 'var(--dangerColor, red)' }}>No cookie file</span>

            <SpinnerButton
              kind={kinds.DEFAULT}
              isSpinning={!isNew && isUploading}
              onPress={() => fileInputRef.current?.click()}
            >
              Upload
            </SpinnerButton>
          </>
        )}

        <input
          ref={fileInputRef}
          type="file"
          accept=".txt"
          style={{ display: 'none' }}
          onChange={handleFileChange}
        />
      </div>

      {!isNew && uploadError != null && (
        <Alert kind="danger">
          Upload failed: {uploadError.statusText}
        </Alert>
      )}
    </div>
  );
}

export default CookieUploadSection;
