
using System;
using UnityEngine;

namespace CPS
{
    public class TextureSet : IDisposable
    {
        public RenderTexture Position { get; private set; }
        public RenderTexture Velocity { get; private set; }
        public RenderTexture Color { get; private set; }
        public RenderTexture Occupancy { get; private set; }

        public TextureSet(int width, int height)
        {
            Position = CreateTexture(width, height, RenderTextureFormat.ARGBFloat);
            Velocity = CreateTexture(width, height, RenderTextureFormat.ARGBFloat);
            Color = CreateTexture(width, height, RenderTextureFormat.ARGB32);
            Occupancy = CreateTexture(width, height, RenderTextureFormat.RInt);
        }

        private RenderTexture CreateTexture(int width, int height, RenderTextureFormat format)
        {
            var rt = new RenderTexture(width, height, 0, format)
            {
                enableRandomWrite = true,
                useMipMap = false,
                filterMode = FilterMode.Point
            };
            rt.Create();
            return rt;
        }

        public void Dispose()
        {
            Position?.Release();
            Velocity?.Release();
            Color?.Release();
            Occupancy?.Release();
        }
    }
}
