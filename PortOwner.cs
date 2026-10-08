using System.Net;
using System.Runtime.InteropServices;
namespace ClaudeSkin;
internal static class PortOwner
{
    [DllImport("iphlpapi.dll",SetLastError=true)] static extern uint GetExtendedTcpTable(IntPtr table,ref int size,bool order,int family,int tableClass,uint reserved);
    public static int? Find(int port)
    {
        int size=0; GetExtendedTcpTable(IntPtr.Zero,ref size,false,2,3,0);
        if (size<=0) return null;
        var buffer=Marshal.AllocHGlobal(size);
        try {
            if (GetExtendedTcpTable(buffer,ref size,false,2,3,0)!=0) return null;
            var count=Marshal.ReadInt32(buffer);
            for(int i=0;i<count;i++) {
                var row=IntPtr.Add(buffer,4+i*24);
                var state=Marshal.ReadInt32(row);
                var address=new IPAddress(unchecked((uint)Marshal.ReadInt32(row,4)));
                var raw=Marshal.ReadInt32(row,8);
                var localPort=((raw&255)<<8)|((raw>>8)&255);
                if (state==2 && localPort==port && IPAddress.IsLoopback(address)) return Marshal.ReadInt32(row,20);
            }
            return null;
        } finally { Marshal.FreeHGlobal(buffer); }
    }
}
