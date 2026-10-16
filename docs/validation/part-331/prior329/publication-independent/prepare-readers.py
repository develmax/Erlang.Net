from pathlib import Path
r=Path.cwd();old=r/'artifacts/part328-remote-beam310';new=r/'artifacts/part329-remote-new-review'
for name in ['passive-common.py','review-allocator-profile.py','review-memory-profile.py']:
 s=(old/name).read_text().replace('part328-remote-beam310','part329-remote-new-review').replace('HEAD328','HEAD329')
 (new/name).write_bytes(s.encode())
s=(old/'review-ci.py').read_text().replace('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612','d83ed875d0c77939e20049afcb46fa297fee82dd').replace('38865058842','38899129114')
s=s.replace('part-328','PART_CURRENT').replace('part-327','part-328').replace('PART_CURRENT','part-329').replace('6738','CURRENT_COUNT').replace('6729','6738').replace('CURRENT_COUNT','6747').replace('root-part328','root-part329')
s=s.replace('1F6C9200E8DEA6DC977174637990EBF409C7E18EFF1B663AC8ABB6A2E808235C','15458BB90CB4D8BCE27F4EC07E1D91BBF3ECF6A05E293BCC0AACCC080236939A').replace('(1365,29,24)','(1170,12,7)').replace('BA9F82F848896139AC2D4BC81FCA724C7E0A9F145A1622B838A126B3D2228542','FFBF65596ED330ACF20F82FC2BFBB3F319824A4B54184AB8ECCE2F66E4757F52').replace('EB815424998D58648C17F79F4D5BFBB775C131ACDAC2C4AF083D46B65EC114D6','C6A8AFDB13B0E6241A765FFFFA48808489626CA8D6138093692CB2027D755D17').replace('B86A971D4922D1F20C97BACDF08A7A99534932CC8EA29FB2DACFEDC444084302','911C51D20E8D98B19CE85B40947C4A6D4CBBF245BDF38452C0ABA7ED56C65C64').replace('PUBLISHED328','PUBLISHED329').replace('1365_COMMITTED','1170_COMMITTED').replace('Previous327','Previous328').replace('PublishedStagePathsRehashed=1365','PublishedStagePathsRehashed=1170').replace('PublishedSourcePaths=29','PublishedSourcePaths=12').replace('PublishedCSharpRawExact=24','PublishedCSharpRawExact=7')
(new/'review-ci.py').write_bytes(s.encode())
print('Prepared local passive CI and profile readers; not executed against missing artifacts')
