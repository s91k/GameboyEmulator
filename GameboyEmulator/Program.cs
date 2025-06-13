using System;
using System.Net.Quic;
using SDL3;

namespace GameboyEmulator
{
    internal class Program
    {
        private const int WIDTH = 160;
        private const int HEIGHT = 144;

        private const int SCALE = 4;


        static void Main(string[] args)
        {
            var memory = new byte[3];

            memory[0] = 0b_00000000;
            memory[1] = 0b_00000000;
            memory[2] = 0b_00000000;

            CPU cpu = new CPU(memory);

            cpu.Run(3);

            /*SDL.Init(SDL.InitFlags.Video);

            if(!SDL.CreateWindowAndRenderer("Gameboy Emulator", WIDTH * SCALE, HEIGHT * SCALE, SDL.WindowFlags.InputFocus, out var window, out var renderer))
            {
                Console.WriteLine($"Could not create window, {SDL.GetError()}");
                SDL.Quit();
            }

            var texture = SDL.CreateTexture(renderer, SDL.PixelFormat.ARGB8888, SDL.TextureAccess.Streaming, WIDTH, HEIGHT);

            byte[] frameBuffer = new byte[WIDTH * HEIGHT * 4];

            for(var x = 0; x < WIDTH; x++)
            {
                for(var y = 0; y < HEIGHT; y++)
                {
                    var offset = (y * WIDTH + x) * 4;

                    frameBuffer[offset + 0] = 0x00;
                    frameBuffer[offset + 1] = 0xFF;
                    frameBuffer[offset + 2] = 0x00;
                    frameBuffer[offset + 3] = 0xFF;
                }
            }

            bool running = true;

            while (running)
            {
                while(SDL.PollEvent(out var e))
                {
                    if(e.Type == (uint)SDL.EventType.Quit)
                    {
                        running = false;
                    }
                }

                SDL.RenderClear(renderer);

                SDL.UpdateTexture(texture, IntPtr.Zero, frameBuffer, WIDTH * 4);

                SDL.RenderTexture(renderer, texture, IntPtr.Zero, IntPtr.Zero);

                SDL.RenderPresent(renderer);

                SDL.Delay(16);
            }

            SDL.DestroyTexture(texture);
            SDL.DestroyRenderer(renderer);
            SDL.DestroyWindow(window);
            SDL.Quit();*/
        }
    }
}
