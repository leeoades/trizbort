# Why Recast isn't signed yet, and how you can help

[Back to the guide](index.md)

The downloads aren't signed yet. An unsigned app has no publisher that Windows or macOS can check, so they warn about it
before it first runs:

- **Windows** shows "Windows protected your PC". Choose **More info**, then **Run anyway**. It only asks the first time.
- **macOS** refuses to open it the first time. Go to **System Settings > Privacy & Security** and choose
  **Open Anyway**.

Some antivirus tools have also quarantined Trizbort as suspicious, sometimes in the middle of mapping a game. Trizbort
contains no malware: it's built from the public source on [GitHub](https://github.com/leeoades/trizbort) by GitHub
Actions, and it collects nothing about you (see the
[privacy policy](https://github.com/leeoades/trizbort/blob/master/PRIVACY.md)).

## Why it isn't signed

We plan to sign the Windows release through the [SignPath Foundation](https://signpath.org), which signs open-source
projects for free. A signed app names its publisher, proves the download hasn't been changed since it was built, and
builds up a good reputation with Windows and antivirus tools over time, so the warnings fade. The Foundation only signs
projects with an established public following, and this repository is new, so it isn't eligible yet. We'll apply
again once it is.

## How you can help

- **Star the repository** on [GitHub](https://github.com/leeoades/trizbort). Stars are one of the first things the
  Foundation looks at.
- **Use it, and tell people about it**: on [intfiction.org](https://intfiction.org), Reddit, Discord, blogs or
  videos. Links and discussions from elsewhere count for a lot.
- **Report bugs and suggest features** in the [issues](https://github.com/leeoades/trizbort/issues), and send pull
  requests: an active project with contributors is a trusted one.
- **Report false positives** if your antivirus flags Trizbort, to its maker (and to Microsoft at
  [microsoft.com/wdsi/filesubmission](https://www.microsoft.com/wdsi/filesubmission)), so it's cleared for everyone.
- **Add it to lists** of interactive fiction tools, such as the [IFWiki](https://www.ifwiki.org) page for Trizbort.

Thank you to everyone who stars, shares, tests and reports: every bit of it helps Trizbort reach more players and
authors, and gets us closer to a signed app that installs without the warnings.
