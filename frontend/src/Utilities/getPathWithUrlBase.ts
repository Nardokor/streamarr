export default function getPathWithUrlBase(path: string) {
  return `${window.Streamarr.urlBase}${path}`;
}
