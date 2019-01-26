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
        private readonly int stride;

        private T[] data;
        private int count, allocated;

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

            this.data = new T[4];
            this.count = 0;

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, this.binding, this.Handle);
        }

        public void Allocate(int count)
        {
            if (count < this.allocated)
            {
                return;
            }

            this.Bind();

            int size = this.stride * count / 1024;
            string type = this.data[0].GetType().Name;
            Console.WriteLine($"Allocating {size}K for Buffer<{type}> (binding={this.binding}, stride={this.stride}, count={this.count})");

            GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    this.stride * count,
                    IntPtr.Zero,
                    BufferUsageHint.StaticRead);

            this.allocated = count;
        }

        public void CopyFromDevice()
        {
            this.data = new T[this.allocated];

            this.Bind();

            GL.GetBufferSubData(
                BufferTarget.ShaderStorageBuffer,
                IntPtr.Zero,
                this.stride * this.allocated,
                this.data);
        }

        public void CopyToDevice()
        {
            if (this.count == 0)
            {
                return;
            }

            this.Bind();

            int size = this.stride * this.count / 1024;
            string type = this.data[0].GetType().Name;

            Console.WriteLine($"Transfering {size}K from Buffer<{type}> (binding={this.binding}, stride={this.stride}, count={this.count})");

            if (this.count == this.allocated)
            {
                GL.BufferSubData(
                    BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero,
                    this.stride * this.count,
                    this.data);
            }
            else
            {
                GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    this.stride * this.count,
                    this.data,
                    BufferUsageHint.DynamicDraw);

                this.allocated = this.count;
            }
        }

        public ref T GetReference(int index)
        {
            return ref this.data[index];
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(this.data, item, 0, this.count);
        }

        public void Add(T item)
        {
            this.EnsureCapacity(this.count + 1);
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
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);

            if (index == -1)
            {
                return false;
            }

            this.RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            this.count--;

            Array.Copy(this.data, index + 1, this.data, index, this.count - index);
        }

        public void Insert(int index, T item)
        {
            this.EnsureCapacity(this.count + 1);

            if (index < this.count)
            {
                Array.Copy(this.data, index, this.data, index + 1, this.count - index);
            }

            this.data[index] = item;
            this.count++;
        }

        public bool Contains(T item)
        {
            return this.IndexOf(item) != -1;
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(this.data, index, count, comparer);
        }

        protected override void BindGraphicsResource()
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this.Handle);
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteBuffer(this.Handle);
        }

        private void EnsureCapacity(int min)
        {
            if (this.data.Length < min)
            {
                int capacity = this.count * 2;

                if (capacity < min)
                {
                    capacity = min;
                }

                T[] newdata = new T[capacity];
                Array.Copy(this.data, newdata, this.count);

                this.data = newdata;
            }
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
