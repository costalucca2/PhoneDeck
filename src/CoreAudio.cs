using System; using System.IO; using System.Diagnostics; using System.Runtime.InteropServices; using System.Collections.Generic; using System.Windows.Forms; using System.Drawing;
namespace PhoneDeckAudio {
 [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
 [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevices {
  [PreserveSig] int EnumAudioEndpoints(int flow,int state,out ICollection devices);
  [PreserveSig] int GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
  [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
  [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client); [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
 }
 [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ICollection { [PreserveSig] int GetCount(out uint count); [PreserveSig] int Item(uint index,out IDevice device); }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice {
  [PreserveSig] int Activate(ref Guid iid,int context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object instance);
  [PreserveSig] int OpenPropertyStore(int access,out IntPtr properties); [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id); [PreserveSig] int GetState(out int state);
 }
 [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IEndpointVolume {
  [PreserveSig] int RegisterControlChangeNotify(IntPtr p);[PreserveSig] int UnregisterControlChangeNotify(IntPtr p);[PreserveSig] int GetChannelCount(out uint count);[PreserveSig] int SetMasterVolumeLevel(float level,ref Guid context);[PreserveSig] int SetMasterVolumeLevelScalar(float level,ref Guid context);[PreserveSig] int GetMasterVolumeLevel(out float level);[PreserveSig] int GetMasterVolumeLevelScalar(out float level);
  [PreserveSig] int SetChannelVolumeLevel(uint channel,float level,ref Guid context);[PreserveSig] int SetChannelVolumeLevelScalar(uint channel,float level,ref Guid context);[PreserveSig] int GetChannelVolumeLevel(uint channel,out float level);[PreserveSig] int GetChannelVolumeLevelScalar(uint channel,out float level);[PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);[PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
 }
}
