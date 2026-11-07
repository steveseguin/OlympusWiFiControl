# UDP sequence regression

In a fresh Windows PowerShell or PowerShell process, run:

```powershell
./tests/run-sequence-regression.ps1
```

The runner compiles the production `UDPInputStream.cs` with small test doubles
for WPF and `Picture`, then calls its actual private packet-processing method.
It never starts the socket listener or sends camera commands.

Coverage includes every low-byte transition, continuous packets crossing the
low-byte and full 16-bit sequence wraps, and packet gap/duplicate recovery.
On the original code the 254 -> 255 assertion is expected to fail.

This only checks packet acceptance and partial-picture reset behavior. It does
not validate the real `Picture` parser, JPEG decoding, WPF dispatch, network
reception, or live camera operation. A full Windows build and a camera live-view
test are still required before merging.
