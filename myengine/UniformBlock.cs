﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;


using vec2 = OpenTK.Vector2;
using vec3 = OpenTK.Vector3;
using dvec3 = OpenTK.Vector3d;
using mat4 = OpenTK.Matrix4;

namespace MyEngine
{
    public class UniformBlock
    {

        // std140 layout and uniform buffer objects http://www.opentk.com/node/2926
        // block 4N (N = sizeof(float))
        // if the next defined variable in a block cannot fit within the size of the remainder of the chunk 
        // then the values are aligned with the next chunk.

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct EngineUniformStruct
        {
            public mat4 viewMatrix;
            public mat4 projectionMatrix;
            public mat4 viewProjectionMatrix;
            public vec3 cameraPosition;
            public int numberOfLights;
            public vec2 screenSize;
            public float nearClipPlane;
            public float farClipPlane;
            public vec3 ambientColor;
            public float totalElapsedSecondsSinceEngineStart;
            public float gammaCorrectionTextureRead;
            public float gammaCorrectionFinalColor;
        }
        public EngineUniformStruct engine;
        public UniformBufferObject<EngineUniformStruct> engineUBO;

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ModelUniformStruct
        {
            public mat4 modelMatrix;
            public mat4 modelViewMatrix;
            public mat4 modelViewProjectionMatrix;
			public vec3 worldPosition;
		}
        public ModelUniformStruct model;
        public UniformBufferObject<ModelUniformStruct> modelUBO;


        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct LightUniformStruct
        {
            public vec3 color;
            public float spotExponent;
            public vec3 position; // position == 0,0,0 => directional light 
            public float spotCutOff;
            public vec3 direction; // direction == 0,0,0 => point light
            public int hasShadows;
            public int lightIndex;

        }
        public LightUniformStruct light;
        public UniformBufferObject<LightUniformStruct> lightUBO;


        int maxUniformIndex;
        int nextUniformIndex = 0;

        public UniformBlock()
        {
            GL.GetInteger(GetPName.MaxUniformBufferBindings, out maxUniformIndex); MyGL.Check();
            engineUBO = new UniformBufferObject<EngineUniformStruct>(nextUniformIndex++, () => engine);
            modelUBO = new UniformBufferObject<ModelUniformStruct>(nextUniformIndex++, () => model);
            lightUBO = new UniformBufferObject<LightUniformStruct>(nextUniformIndex++, () => light);
        }

        public void SetUniformBuffers(Shader shader)
        {
            shader.SetUniformBlockBufferIndex("engine_block", engineUBO.GetBufferIndex());
            shader.SetUniformBlockBufferIndex("model_block", modelUBO.GetBufferIndex());
            shader.SetUniformBlockBufferIndex("light_block", lightUBO.GetBufferIndex());
        }



        public class UniformBufferObject<T> where T : struct
        {

            int bufferUBO; // Location for the UBO given by OpenGL
            int bufferIndex; // Index to use for the buffer binding (All good things start at 0 )
            int size;
            Func<T> getData;
            public UniformBufferObject(int index, Func<T> getData)
            {
                bufferIndex = index;
                this.getData = getData;
                size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

                bufferUBO = GL.GenBuffer(); MyGL.Check(); // Generate the buffer
                GL.BindBuffer(BufferTarget.UniformBuffer, bufferUBO); MyGL.Check(); // Bind the buffer for writing
                GL.BufferData(BufferTarget.UniformBuffer, (IntPtr)(size), (IntPtr)(null), BufferUsageHint.StreamDraw); MyGL.Check(); // Request the memory to be allocated
                GL.BindBufferRange(BufferRangeTarget.UniformBuffer, bufferIndex, bufferUBO, (IntPtr)0, size); MyGL.Check();
                GL.BindBuffer(BufferTarget.UniformBuffer, 0); MyGL.Check(); //unbind
            }


            public void UploadToGPU()
            {
                T d = getData();
                GL.BindBuffer(BufferTarget.UniformBuffer, bufferUBO); MyGL.Check();
                GL.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)0, size, ref d); MyGL.Check();
                GL.BindBuffer(BufferTarget.UniformBuffer, 0); MyGL.Check(); //unbind
            }


            public int GetBufferIndex()
            {
                return bufferIndex;
            }
        }
    }
}
