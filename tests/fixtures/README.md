# Test Fixtures

Do not commit commercial or otherwise copyrighted `.smk` files to this repository.

Unit tests should use tiny synthetic byte arrays checked into source. Real `.smk` files are for opt-in sample and conformance tests.

Supported local fixture configuration:

- `LIBSMACKER_SAMPLE_DIR`: directory containing one or more local `.smk` files.
- `LIBSMACKER_HELLO_SMK`: full path to the Dark Reign `HELLO.SMK` sample.
- `LIBSMACKER_ROUNDTRIP_MP4`: full path to a local MP4 used for round-trip verification.
- `LIBSMACKER_FFMPEG`: full path to `ffmpeg.exe` when it is not on `PATH`.

Known local sample:

```text
C:\Program Files (x86)\GOG Galaxy\Games\Dark Reign\dark\movies\HELLO.SMK
```

Sample tests should skip themselves when the configured files are missing. They should not fail a normal clean checkout.
