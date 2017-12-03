using System;

namespace PhotoSorterUtility
{
    public sealed class ExifTag
    {
        public Int64 Id { get; set; }
        public String Name { get; set ;}
        public String Value { get; set; }

        #region Example list of tags
        /**
            0x010f Make: LGE
            0x0110 Model: Nexus 5
            0x011a XResolution: 72
            0x011b YResolution: 72
            0x0128 ResolutionUnit: 2
            0x0213 YCbCrPositioning: 1
            0x829a ExposureTime: 0.03333333333
            0x829d FNumber: 2.4
            0x8827 ISO: 946
            0x9000 ExifVersion: 0220
            0x9003 DateTimeOriginal: 2014:02:16 12:04:43
            0x9004 CreateDate: 2014:02:16 12:04:43
            0x9101 ComponentsConfiguration: 1 2 3 0
            0x9201 ShutterSpeedValue: 0.0333308056509926
            0x9202 ApertureValue: 2.39495740923786
            0x9204 ExposureCompensation: 0
            0x9209 Flash: 0
            0x920a FocalLength: 3.97
            0xa000 FlashpixVersion: 0100
            0xa001 ColorSpace: 1
            0xa002 ExifImageWidth: 1944
            0xa003 ExifImageHeight: 2592
            0x0001 InteropIndex: R98
            0x0002 InteropVersion: 0100
            0x0103 Compression: 6
            0x0201 ThumbnailOffset: 556
            0x0202 ThumbnailLength: 14345
            0x0112 Orientation: 1
            0x0131 Software: Google
        */
        #endregion Example of list of tags
    }
}
