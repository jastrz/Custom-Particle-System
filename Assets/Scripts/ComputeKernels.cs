using UnityEngine;

namespace CPS
{
    public class ComputeKernels
    {
        public int Init { get; private set; }
        public int Simulate { get; private set; }
        public int Add { get; private set; }
        public int Remove { get; private set; }
        public int Attract { get; private set; }
        public int AttractFromData { get; private set; }

        private ComputeShader shader;

        public ComputeKernels(ComputeShader computeShader)
        {
            shader = computeShader;

            Init = shader.FindKernel("CSInit");
            Simulate = shader.FindKernel("CSSimulate");
            Add = shader.FindKernel("CSAddSand");
            Remove = shader.FindKernel("CSRemoveSand");
            Attract = shader.FindKernel("CSAttract");
            AttractFromData = shader.FindKernel("CSAttractFromData");
        }

        public void SetTexturesForInit(TextureSet textures)
        {
            shader.SetTexture(Init, "PositionWrite", textures.Position);
            shader.SetTexture(Init, "VelocityWrite", textures.Velocity);
            shader.SetTexture(Init, "ColorWrite", textures.Color);
            shader.SetTexture(Init, "OccupancyRead", textures.Occupancy);
            shader.SetTexture(Init, "OccupancyWrite", textures.Occupancy);
        }

        public void SetTexturesForSimulate(TextureSet read, TextureSet write)
        {
            shader.SetTexture(Simulate, "PositionRead", read.Position);
            shader.SetTexture(Simulate, "VelocityRead", read.Velocity);
            shader.SetTexture(Simulate, "ColorRead", read.Color);
            shader.SetTexture(Simulate, "OccupancyRead", read.Occupancy);

            shader.SetTexture(Simulate, "PositionWrite", write.Position);
            shader.SetTexture(Simulate, "VelocityWrite", write.Velocity);
            shader.SetTexture(Simulate, "ColorWrite", write.Color);
            shader.SetTexture(Simulate, "OccupancyWrite", write.Occupancy);
        }

        public void SetTexturesForAdd(TextureSet textures)
        {
            shader.SetTexture(Add, "PositionWrite", textures.Position);
            shader.SetTexture(Add, "VelocityWrite", textures.Velocity);
            shader.SetTexture(Add, "ColorWrite", textures.Color);
            shader.SetTexture(Add, "OccupancyRead", textures.Occupancy);
            shader.SetTexture(Add, "OccupancyWrite", textures.Occupancy);
        }

        public void SetTexturesForRemove(TextureSet textures)
        {
            shader.SetTexture(Remove, "PositionWrite", textures.Position);
            shader.SetTexture(Remove, "VelocityWrite", textures.Velocity);
            shader.SetTexture(Remove, "ColorWrite", textures.Color);
            shader.SetTexture(Remove, "OccupancyRead", textures.Occupancy);
            shader.SetTexture(Remove, "OccupancyWrite", textures.Occupancy);
        }

        public void SetTexturesForAttract(TextureSet read, TextureSet write)
        {
            shader.SetTexture(Attract, "PositionRead", read.Position);
            shader.SetTexture(Attract, "VelocityRead", read.Velocity);
            shader.SetTexture(Attract, "OccupancyRead", read.Occupancy);

            shader.SetTexture(Attract, "PositionWrite", write.Position);
            shader.SetTexture(Attract, "VelocityWrite", write.Velocity);
            shader.SetTexture(Attract, "OccupancyWrite", write.Occupancy);
        }

        public void SetTexturesForAttractFromData(TextureSet read, TextureSet write)
        {
            shader.SetTexture(AttractFromData, "PositionRead", read.Position);
            shader.SetTexture(AttractFromData, "VelocityRead", read.Velocity);
            shader.SetTexture(AttractFromData, "OccupancyRead", read.Occupancy);

            shader.SetTexture(AttractFromData, "PositionWrite", write.Position);
            shader.SetTexture(AttractFromData, "VelocityWrite", write.Velocity);
            shader.SetTexture(AttractFromData, "OccupancyWrite", write.Occupancy);
        }
    }
}
