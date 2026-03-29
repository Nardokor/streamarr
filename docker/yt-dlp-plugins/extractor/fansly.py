import json
import re

from yt_dlp.extractor.common import InfoExtractor
from yt_dlp.utils import ExtractorError
from yt_dlp.utils.traversal import traverse_obj


class FanslyBaseIE(InfoExtractor):
    _NETRC_MACHINE = 'fansly'

    def _initialize_pre_login(self):
        self._auth_headers = {}

    def _perform_login(self, username, password):
        devid = self._download_json(
            'https://apiv3.fansly.com/api/v1/device/id?ngsw-bypass=true',
            None, impersonate=True).get('response')
        res = self._download_json(
            'https://apiv3.fansly.com/api/v1/login?ngsw-bypass=true',
            None, impersonate=True, data=json.dumps({
                'deviceId': devid,
                'username': username,
                'password': password,
            }).encode())
        if traverse_obj(res, ('response', 'twofa')):
            token = traverse_obj(res, ('response', 'twofa', 'token'))
            code = self._get_tfa_info(
                f'two-factor verification code sent to {traverse_obj(res, ("response", "twofa", "email"))}')
            tfa_res = self._download_json(
                'https://apiv3.fansly.com/api/v1/login/twofa?ngsw-bypass=true',
                None, impersonate=True, data=json.dumps({
                    'token': token,
                    'code': code,
                }).encode())
            self._auth_headers = {'Authorization': traverse_obj(tfa_res, ('response', 'token'))}
        else:
            self._auth_headers = {'Authorization': traverse_obj(res, ('response', 'session', 'token'))}


class FanslyLiveIE(FanslyBaseIE):
    _VALID_URL = r'https?://(?:www\.)?fansly\.com/live/(?P<id>[0-9a-zA-Z_]+)'
    _TESTS = [{
        'url': 'https://fansly.com/live/YuukoVT',
        'info_dict': {
            'id': '622010656702345216',
            'channel_id': '530029089403183104',
            'ext': 'mp4',
        },
        'params': {'skip_download': True},
        'skip': 'Channel is not live',
    }, {
        'url': 'https://fansly.com/live/284824898138812416',
        'info_dict': {
            'id': '563252644517257217',
            'channel_id': '284824898138812416',
            'ext': 'mp4',
        },
        'params': {'skip_download': True},
        'skip': 'Channel is not live',
    }]

    def _real_extract(self, url):
        video_id = self._match_id(url)
        if not video_id.isdigit():
            user = self._download_json(
                f'https://apiv3.fansly.com/api/v1/account?usernames={video_id}&ngsw-bypass=true',
                video_id, impersonate=True)
            if not user.get('success') or len(user.get('response', [])) == 0:
                raise ExtractorError('Failed to get channel ID')
            video_id = user['response'][0]['id']

        channel = self._download_json(
            f'https://apiv3.fansly.com/api/v1/streaming/channel/{video_id}?ngsw-bypass=true',
            video_id, impersonate=True, headers=self._auth_headers)
        if not channel.get('success') or not channel.get('response'):
            raise ExtractorError('Failed to get channel info')
        stream = traverse_obj(channel, ('response', 'stream'))
        if not stream or not stream.get('startedAt'):
            raise ExtractorError('Channel is not live', expected=True)
        if not stream.get('playbackUrl'):
            raise ExtractorError('No playback URL — the stream may require a subscription', expected=True)

        started_at = stream.get('startedAt')
        updated_at = stream.get('updatedAt')
        return {
            'id': stream.get('id'),
            'title': stream.get('title'),
            'formats': self._extract_m3u8_formats(stream.get('playbackUrl'), video_id, ext='mp4', live=True),
            'timestamp': started_at / 1000 if started_at else None,
            'modified_timestamp': updated_at / 1000 if updated_at else None,
            'channel_id': stream.get('accountId'),
            'concurrent_view_count': stream.get('viewerCount'),
            'age_limit': 18,
            'is_live': True,
        }


class FanslyIE(FanslyBaseIE):
    _VALID_URL = r'https?://(?:www\.)?fansly\.com/post/(?P<id>[0-9]+)'
    _TESTS = [{
        'url': 'https://fansly.com/post/713619348626874370',
        'info_dict': {
            'id': '713619348626874370',
            'ext': 'mp4',
        },
        'params': {'skip_download': True},
    }]

    def _real_extract(self, url):
        video_id = self._match_id(url)
        res = self._download_json(
            f'https://apiv3.fansly.com/api/v1/post?ids={video_id}&ngsw-bypass=true',
            video_id, impersonate=True, headers=self._auth_headers)
        if not res.get('success') or not res.get('response'):
            raise ExtractorError('Failed to get post info')
        try:
            post = traverse_obj(res, ('response', 'posts'))[0]
        except IndexError:
            raise ExtractorError('Could not find post')
        try:
            account = traverse_obj(res, ('response', 'accounts'))[0]
        except IndexError:
            raise ExtractorError('Could not find account info for post')

        playlist = []
        for media in traverse_obj(res, ('response', 'accountMedia', lambda _, v: v['media'])):
            thumbnail = None
            m = media['media']
            metadata = json.loads(m.get('metadata') or '{}')
            media_id = media.get('mediaId')

            try:
                formats = [{
                    'url': m.get('locations')[0].get('location'),
                    'format_id': str(m.get('type')),
                    'width': m.get('width'),
                    'height': m.get('height'),
                    'fps': metadata.get('frameRate'),
                    'http_headers': {
                        'Cookie': '; '.join(
                            'CloudFront-' + k + '=' + v
                            for k, v in m['locations'][0]['metadata'].items()),
                    } if m.get('locations')[0].get('metadata') else {},
                }]
                for variant in traverse_obj(m, ('variants', lambda _, v: v['mimetype'] and v['locations'])):
                    mimetype = variant.get('mimetype')
                    try:
                        location = variant['locations'][0]
                    except IndexError:
                        self.report_warning(
                            f'Could not get variant location for ID {variant.get("id")}, '
                            'skipping (you may need to log in)', video_id)
                        continue
                    headers = {
                        'Cookie': '; '.join(
                            'CloudFront-' + k + '=' + v
                            for k, v in location['metadata'].items()),
                    } if location.get('metadata') else {}
                    if mimetype == 'application/vnd.apple.mpegurl':
                        f = self._extract_m3u8_formats(location.get('location'), media_id, ext='mp4', headers=headers)
                        for fmt in f:
                            fmt['http_headers'] = headers
                        formats.extend(f)
                    elif mimetype == 'application/dash+xml':
                        f = self._extract_mpd_formats(location.get('location'), media_id, headers=headers)
                        for fmt in f:
                            fmt['http_headers'] = headers
                        formats.extend(f)
                    elif mimetype == 'image/jpeg':
                        if thumbnail is None:
                            thumbnail = location.get('location')
                    else:
                        vmetadata = json.loads(variant.get('metadata') or '{}')
                        formats.append({
                            'url': location.get('location'),
                            'format_id': str(variant.get('type')),
                            'width': variant.get('width'),
                            'height': variant.get('height'),
                            'fps': vmetadata.get('frameRate') or metadata.get('frameRate'),
                            'http_headers': headers,
                        })
            except IndexError:
                self.report_warning(
                    f'Could not get media locations for ID {m.get("id")}. '
                    'You may need to authenticate with --username and --password.', video_id)
                formats = []

            if any(
                v.get('price', 0) > 0 or (v.get('metadata') not in ('', '{}', None))
                for v in (traverse_obj(media, ('permissions', 'permissionFlags')) or [])
            ):
                availability = 'premium_only'
            elif not formats:
                availability = 'needs_auth'
            else:
                availability = 'public'

            created_at = m.get('createdAt')
            media_created_at = media.get('createdAt')
            updated_at = m.get('updatedAt')
            playlist.append({
                'id': media_id,
                'title': '',
                'formats': formats,
                'thumbnail': thumbnail,
                'uploader': account.get('username'),
                'timestamp': created_at / 1000 if created_at else None,
                'release_timestamp': media_created_at / 1000 if media_created_at else None,
                'modified_timestamp': updated_at / 1000 if updated_at else None,
                'uploader_id': post.get('accountId'),
                'uploader_url': 'https://fansly.com/' + account.get('username'),
                'channel_id': post.get('accountId'),
                'channel_url': 'https://fansly.com/' + account.get('username'),
                'channel_follower_count': account.get('followCount'),
                'location': account.get('location'),
                'duration': metadata.get('duration'),
                'like_count': media.get('likeCount'),
                'comment_count': post.get('replyCount') or 0,
                'age_limit': 18,
                'tags': re.findall(r'#[^ ]+', post.get('content') or ''),
                'is_live': False,
                'availability': availability,
            })

        return self.playlist_result(playlist, video_id, playlist_description=post.get('content'))
