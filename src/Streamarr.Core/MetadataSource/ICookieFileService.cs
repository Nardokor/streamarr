namespace Streamarr.Core.MetadataSource
{
    public interface ICookieFileService
    {
        /// <summary>
        /// Saves the cookie file bytes for the given source definition ID.
        /// Returns the absolute path where the file was written.
        /// </summary>
        string Save(int definitionId, byte[] content);

        /// <summary>
        /// Deletes the cookie file for the given source definition ID.
        /// Does nothing if the file does not exist.
        /// </summary>
        void Delete(int definitionId);

        /// <summary>
        /// Returns true if a cookie file exists for the given source definition ID.
        /// </summary>
        bool Exists(int definitionId);

        /// <summary>
        /// Returns the expected path for the cookie file regardless of whether it exists.
        /// </summary>
        string GetPath(int definitionId);
    }
}
