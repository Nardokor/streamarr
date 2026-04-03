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
  sourceId: number;
}

function CookieUploadSection({ sourceId }: CookieUploadSectionProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { data: status } = useCookieStatus(sourceId);
  const { mutate: upload, isPending: isUploading, error: uploadError } = useUploadCookies(sourceId);
  const { mutate: remove, isPending: isRemoving } = useDeleteCookies(sourceId);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      upload(file);
    }

    // Reset so the same file can be re-uploaded if needed.
    e.target.value = '';
  };

  return (
    <div style={{ marginTop: '16px', borderTop: '1px solid var(--borderColor)', paddingTop: '16px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
        <strong style={{ minWidth: '120px' }}>Cookies</strong>

        {status?.hasCookies ? (
          <>
            <span style={{ color: 'var(--successColor, green)' }}>Cookie file uploaded</span>

            <SpinnerButton
              kind={kinds.DEFAULT}
              isSpinning={isUploading}
              onPress={() => fileInputRef.current?.click()}
            >
              Replace
            </SpinnerButton>

            <SpinnerButton
              kind={kinds.DANGER}
              isSpinning={isRemoving}
              onPress={() => remove()}
            >
              Remove
            </SpinnerButton>
          </>
        ) : (
          <>
            <span style={{ color: 'var(--dangerColor, red)' }}>No cookie file</span>

            <SpinnerButton
              kind={kinds.DEFAULT}
              isSpinning={isUploading}
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

      {uploadError != null && (
        <Alert kind="danger">
          Upload failed: {uploadError.statusText}
        </Alert>
      )}
    </div>
  );
}

export default CookieUploadSection;
