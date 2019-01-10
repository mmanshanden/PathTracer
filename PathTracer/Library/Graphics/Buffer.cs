using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PathTracer.Library.Graphics
{
    class Buffer<T> : BindableResource, IEnumerable<T>
        where T : struct
    {
        private readonly int binding;
        private T[] data;
        private int count;
        private readonly int stride;

        public int Count => this.count;

        public T this[int index]
        {
            get => this.data[index];
            set => this.data[index] = value;
        }

        public Buffer(int binding)
            : base(GL.GenBuffer(), BufferTarget.ShaderStorageBuffer)
        {
            this.binding = binding;
            this.stride = Marshal.SizeOf<T>();

            this.data = new T[1];
            this.count = 0;

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, this.binding, this.Handle);
        }

        protected override void BindGraphicsResource()
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this.Handle);
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteBuffer(this.Handle);
        }

        public void CopyToDevice()
        {
            this.Bind();

            GL.BufferData(
                BufferTarget.ShaderStorageBuffer,
                this.stride * this.count,
                this.data,
                BufferUsageHint.StaticRead);
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < this.count; i++)
            {
                if (this.data[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Add(T item)
        {
            if (this.count == this.data.Length)
            {
                T[] copy = new T[this.count * 2];
                Array.Copy(this.data, copy, this.count);
                this.data = copy;
            }

            this.data[this.count++] = item;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                this.Add(item);
            }
        }

        public void Clear()
        {
            this.count = 0;
            this.data = new T[1];
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);

            if (index == -1)
            {
                return false;
            }

            for (int i = index; i < this.count - 1; i++)
            {
                this.data[i] = this.data[i + 1];
            }

            this.count--;
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > this.count)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = index; i < this.count - 1; i++)
            {
                this.data[i] = this.data[i + 1];
            }

            this.count--;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < this.count; i++)
            {
                yield return this.data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
