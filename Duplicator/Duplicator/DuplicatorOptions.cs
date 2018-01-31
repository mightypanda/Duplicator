﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX.DXGI;

namespace Duplicator
{
    public class DuplicatorOptions : DictionaryObject
    {
        public const string OutputCategory = "Output";
        public const string InputCategory = "Input";

        public DuplicatorOptions()
        {
            Adapter1 adapter;
            using (var fac = new Factory1())
            {
                adapter = fac.Adapters1.FirstOrDefault(a => !a.Description1.Flags.HasFlag(AdapterFlags.Software));
                if (adapter == null)
                {
                    adapter = fac.Adapters1.First();
                }
            }

            Adapter = adapter.Description.Description;
            Output = adapter.Outputs.First().Description.DeviceName;
            FrameAcquisitionTimeout = 500;
        }

        [DisplayName("Directory Path")]
        [Category(OutputCategory)]
        public virtual string OutputDirectoryPath { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }

        [DisplayName("Video Adapter")]
        [Category(InputCategory)]
        public virtual string Adapter { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }

        [DisplayName("Video Monitor")]
        [Category(InputCategory)]
        [TypeConverter(typeof(DisplayDeviceTypeConverter))]
        public virtual string Output { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }

        [DisplayName("Frame Acquisition Timeout")]
        [Category(InputCategory)]
        public virtual int FrameAcquisitionTimeout
        {
            get => DictionaryObjectGetPropertyValue<int>();
            set
            {
                // we don't want infinite
                DictionaryObjectSetPropertyValue(Math.Max(0, value));
            }
        }

        public Adapter1 GetAdapter()
        {
            using (var fac = new Factory1())
            {
                return fac.Adapters1.FirstOrDefault(a => a.Description.Description == Adapter);
            }
        }

        public Output1 GetOutput()
        {
            using (var adapter = GetAdapter())
            {
                return adapter.Outputs.FirstOrDefault(o => o.Description.DeviceName == Output).QueryInterface<Output1>();
            }
        }

        public static string GetDisplayDeviceName(string deviceName)
        {
            if (deviceName == null)
                throw new ArgumentNullException(nameof(deviceName));

            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
            if (!EnumDisplayDevices(deviceName, 0, ref dd, 0))
                return deviceName;

            return dd.DeviceString;
        }

        private class DisplayDeviceTypeConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var name = value as string;
                if (name != null)
                    return GetDisplayDeviceName(name);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        [Flags]
        private enum DISPLAY_DEVICE_FLAGS
        {
            DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001,
            DISPLAY_DEVICE_MULTI_DRIVER = 0x00000002,
            DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004,
            DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008,
            DISPLAY_DEVICE_VGA_COMPATIBLE = 0x00000010,
            DISPLAY_DEVICE_REMOVABLE = 0x00000020,
            DISPLAY_DEVICE_ACC_DRIVER = 0x00000040,
            DISPLAY_DEVICE_MODESPRUNED = 0x08000000,
            DISPLAY_DEVICE_RDPUDD = 0x01000000,
            DISPLAY_DEVICE_REMOTE = 0x04000000,
            DISPLAY_DEVICE_DISCONNECT = 0x02000000,
            DISPLAY_DEVICE_TS_COMPATIBLE = 0x00200000,
            DISPLAY_DEVICE_UNSAFE_MODES_ON = 0x00080000,
            DISPLAY_DEVICE_ACTIVE = 0x00000001,
            DISPLAY_DEVICE_ATTACHED = 0x00000002,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public DISPLAY_DEVICE_FLAGS StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
    }
}
