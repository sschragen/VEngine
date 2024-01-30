using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;


namespace NaiveSurfaceNets
{
	/// <summary>
	/// Generate SDF
	/// </summary>
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	public struct GenerateJob : IJob, IJobParallelFor
	{
		[NoAlias][WriteOnly][NativeDisableParallelForRestriction] public NativeArray<sbyte> volume;
        [NoAlias][WriteOnly][NativeDisableParallelForRestriction] public NativeArray<byte> material;
        public float time;

		/*
		// used by different modes
		public float3 sphereCenter;
		public float noiseFreq;
		[NoAlias][NativeDisableParallelForRestriction][ReadOnly] public NativeArray<float3> spheresPositions;
		[NoAlias][NativeDisableParallelForRestriction] public NativeArray<float4> spheresDeltas;
		*/

		public void Execute()
		{
			
		}

		public void Execute(int jobIndex)
		{
            TerrainJob(jobIndex);
		}

		private void TerrainJob(int x)
		{
			var flatIndex = x * Chunk.ChunkSize * Chunk.ChunkSize;
			
			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					float2 noisePos = new float2(x, z) + time;
					var val = y - (
						noise.snoise(noisePos * 0.01f) * 8.0f +
						noise.snoise(noisePos * 0.02f) * 4.0f +
						noise.snoise(noisePos * 0.04f) * 2.0f +
						noise.snoise(noisePos * 0.16f) * 0.5f +
						15.5f);
					val = math.clamp(val, -1.0f, 1.0f) * -127;
					volume[flatIndex] = (sbyte)val;


					material[flatIndex] = (byte)1;


					flatIndex++;
				}
			}
		}
	}
}