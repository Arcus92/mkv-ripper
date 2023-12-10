# MkvRipper

This is a personal tool I created to copy my BlueRays to my media server.
The BlueRay must be exported with MakeMKV first. The tool takes the output, converts it to MP4 *(for streaming)* and 
extracts the subtitles.

Subtitle extraction is the heart of this tool. BlueRay supports graphic subtitle *(PGS)*, but MP4 does not. 
The tool builds the subtitle image and runs it through OCR.

## Usage

Start the tool like this:
```./MkvRipper <mkv-input-directory> <mp4-output-directory>```

It offers a very rough console menu.
- Enter `c` to convert all MKV files to MP4 and extract subtitles.
- Enter `f` to guess the forced subtitles and rename the subtitle files accordingly.
- Enter `<number>` to rename a MP4 file with all its subtitles.
- Enter `r` to batch rename all files. Enter a name with `%` for the incremental counter.
- Enter `e` to exit the application.

I only tested it on Linux.

## Requirements

- FFmpeg must be installed.
- MakeMKV to rip the BlueRay disk.
- A lot of memory. The Matroska reader I used will always read the whole MKV file into memory. I need to fine another 
  library that supports partial reads.

