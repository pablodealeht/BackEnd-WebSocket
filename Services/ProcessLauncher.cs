namespace BackEnd_WebSocket.Services
{
    public static class ProcessLauncher
    {
        public static void AbrirNotepads(int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                System.Diagnostics.Process.Start("notepad.exe");
            }
        }
    }
}
