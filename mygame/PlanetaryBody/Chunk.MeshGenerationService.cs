﻿using MyEngine;
using MyEngine.Components;
using OpenTK;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGame.PlanetaryBody
{
	public partial class Chunk
	{
		public class MeshGenerationService
		{
			int generationThreadMiliSecondsSleep;

			HashSet<Chunk> chunkIsBeingGenerated = new HashSet<Chunk>();

			//ReaderWriterLock chunkToPriority_mutex = new ReaderWriterLock();
			Dictionary<Chunk, double> chunkToWeight = new Dictionary<Chunk, double>();

			TimeSpan timeSpentGenerating = TimeSpan.Zero;
			ulong countChunksGenerated;

			List<Thread> threads = new List<Thread>();

			bool doRun;
			Debug debug;

			public MeshGenerationService(Debug debug)
			{
				this.debug = debug;
				Start();
			}

			void Start()
			{
				generationThreadMiliSecondsSleep = 1;
				chunkToWeight.Clear();
				doRun = true;
				int numThreads = Environment.ProcessorCount - 2;
#if DEBUG
				//numThreads = 1;
#endif
				if (numThreads < 1) numThreads = 1;

				for (int i = 0; i < numThreads; i++)
				{
					var threadIndex = i;
					var t = new Thread(() =>
					{
						ThreadMain(threadIndex);
					});
					t.IsBackground = true;
					t.Start();
					threads.Add(t);
				}
			}

			void ThreadMain(int threadIndex)
			{
				while (doRun)
				{
					Chunk chunk = null;

					lock (chunkToWeight)
					{
						if (chunkToWeight.Count > 0)
						{
							double weight = -1;
							foreach (var kvp in chunkToWeight)
							{
								if (kvp.Value > weight)
								{
									weight = kvp.Value;
									chunk = kvp.Key;
								}
							}
							if (chunk != null)
							{
								lock (chunkIsBeingGenerated)
								{
									if (chunkIsBeingGenerated.Contains(chunk))
									{
										chunk = null; // other thread found it faster than this one
									}
									else
									{
										chunkIsBeingGenerated.Add(chunk);
										chunkToWeight.Remove(chunk);
									}
								}
							}
						}
					}

					// this takes alot of time
					if (chunk != null)
					{
						var stopwatch = new System.Diagnostics.Stopwatch();
						stopwatch.Start();
						chunk.CreateRendererAndGenerateMesh();
						countChunksGenerated++;
						timeSpentGenerating = timeSpentGenerating.Add(stopwatch.Elapsed);

						lock (chunkIsBeingGenerated)
						{
							chunkIsBeingGenerated.Remove(chunk);
						}
					}

					if (threadIndex == 0)
					{
						debug.AddValue("generation / chunkd to generate queued", chunkToWeight.Count.ToString());
						debug.AddValue("generation / total chunks generated", countChunksGenerated);
						debug.AddValue("generation / total time spent generating", timeSpentGenerating.TotalSeconds + " s");
						debug.AddValue("generation / average time spent generating", (timeSpentGenerating.TotalSeconds / (float)countChunksGenerated) + " s");


						//if (fps < 55) generationThreadMiliSecondsSleep *= 2;
						//else generationThreadMiliSecondsSleep /= 2;

						generationThreadMiliSecondsSleep = MyMath.Clamp(generationThreadMiliSecondsSleep, 10, 200);
					}
					Thread.Sleep(generationThreadMiliSecondsSleep);
				}
			}

			/// <summary>
			/// Smaller priority is more important.
			/// </summary>
			/// <param name="chunk"></param>
			/// <param name="weight"></param>
			public void RequestGenerationOfMesh(Chunk chunk, double weight)
			{
				if (chunk.renderer != null) return;

				if (chunk.parentChunk != null && chunk.parentChunk.renderer == null)
				{
					chunk.parentChunk.RequestMeshGeneration();
					return;
				}

				lock (chunkIsBeingGenerated)
				{
					var isChunkBeingGenerated = chunkIsBeingGenerated.Contains(chunk);
					if (isChunkBeingGenerated) return;
				}

				if (chunk.renderer != null) return;

				lock (chunkToWeight)
				{
					/*
                    var found = chunkToPriority.ContainsKey(chunk);
                    if (found == false)
                    {
                        chunkToPriority[chunk] = 0;
                    }
                    */
					chunkToWeight[chunk] = weight;
				}
			}

			public void DoesNotNeedMeshGeneration(Chunk chunk)
			{
				lock (chunkToWeight)
				{
					chunkToWeight.Remove(chunk);
				}
			}
		}
	}
}