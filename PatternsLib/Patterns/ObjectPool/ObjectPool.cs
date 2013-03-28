using System;
using System.Collections;
using System.Threading;

namespace PatternsLib.Patterns.ObjectPool
{
    public class ObjectPool<T> where T : class
    {
        /// <summary>
        /// ������ �������������
        /// </summary>
        private readonly Semaphore _semaphore;

        /// <summary>
        /// ��������� �������� ����������� �������
        /// </summary>
        private readonly ArrayList _pool;

        /// <summary>
        /// ������ �� ������, �������� ������������ ��������������� 
        /// �� �������� �������� ����
        /// </summary>
        private readonly ICreation<T> _creator;

        /// <summary>
        /// ���������� ��������, ������������ � ������ ������
        /// </summary>
        private Int32 _instanceCount;

        /// <summary>
        /// ������������ ���������� ����������� ����� ��������
        /// </summary>
        private Int32 _maxInstances;

        /// <summary>
        /// �������� ���� ��������
        /// </summary>
        /// <param name="creator">������, �������� ��� ����� ������������ ���������������
        /// �� �������� ����������� �� ��������</param>
        public ObjectPool(ICreation<T> creator)
            : this(creator, Int32.MaxValue)
        {
        }

        /// <summary>
        /// �������� ���� ��������
        /// </summary>
        /// <param name="creator">������, �������� ��� ����� ������������ ���������������
        /// �� �������� ����������� �� ��������</param>
        /// <param name="maxInstances">������������ ���������� ����������� �����,
        /// ������� ��� ��������� ������������ ������������
        /// </param>
        public ObjectPool(ICreation<T> creator, Int32 maxInstances)
        {
            this._creator = creator;
            this._instanceCount = 0;
            this._maxInstances = maxInstances;
            this._pool = new ArrayList();
            this._semaphore = new Semaphore(0, this._maxInstances);
        }

        /// <summary>
        /// ���������� ���������� �������� � ����, ��������� ����������
        /// �������������. �������� ���������� ����� ���� ������
        /// ����� ��������, ��������� ������������ 
        /// �������� - ��� ���������� "������" ������ � ����.
        /// </summary>
        public Int32 Size
        {
            get
            {
                lock (_pool)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>
        /// ���������� ���������� ����������� ����� ��������,
        /// ������������ � ������ ������
        /// </summary>
        public Int32 InstanceCount
        {
            get { return _instanceCount; }
        }

        /// <summary>
        /// �������� ��� ������ ������������ ���������� ����������� �����
        /// ��������, ������� ��� ��������� ������������ ������������.
        /// </summary>
        public Int32 MaxInstances
        {
            get { return _maxInstances; }
            set { _maxInstances = value; }
        }

        /// <summary>
        /// ���������� �� ���� ������. ��� ������ ���� ����� ������
        /// ������, ���� ���������� ����������� ����� �������� �� 
        /// ������ ��� ����� ��������, ������������� ������� 
        /// <see cref="ObjectPool{T}.MaxInstances"/>. ���� ���������� ����������� ����� 
        /// �������� ��������� ��� ��������, �� ������ ����� ���������� null 
        /// </summary>
        /// <returns></returns>
        public T GetObject()
        {
            lock (_pool)
            {
                T thisObject = RemoveObject();
                if (thisObject != null)
                    return thisObject;

                if (InstanceCount < MaxInstances)
                    return CreateObject();

                return null;
            }
        }

        /// <summary>
        /// ���������� �� ���� ������. ��� ������ ���� ����� ������
        /// ������, ���� ���������� ����������� ����� �������� �� 
        /// ������ ��� ����� ��������, ������������� ������� 
        /// <see cref="ObjectPool{T}.MaxInstances"/>. ���� ���������� ����������� ����� 
        /// �������� ��������� ��� ��������, �� ������ ����� ����� ����� �� ���
        /// ���, ���� �����-������ ������ �� ������ ��������� ���
        /// ���������� �������������.
        /// </summary>
        /// <returns></returns>
        public T WaitForObject()
        {
            lock (_pool)
            {
                T thisObject = RemoveObject();
                if (thisObject != null)
                    return thisObject;

                if (InstanceCount < MaxInstances)
                    return CreateObject();
            }
            _semaphore.WaitOne();
            return WaitForObject();
        }



        /// <summary>
        /// ������� ������ �� ��������� ���� � ���������� ��� 
        /// </summary>
        /// <returns></returns>
        private T RemoveObject()
        {
            while (_pool.Count > 0)
            {
                var refThis = (WeakReference) _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                var thisObject = (T) refThis.Target;
                if (thisObject != null)
                    return thisObject;
                _instanceCount--;
            }
            return null;
        }

        /// <summary>
        /// ������� ������, ����������� ���� �����
        /// </summary>
        /// <returns></returns>
        private T CreateObject()
        {
            T newObject = _creator.Create();
            _instanceCount++;
            return newObject;

        }

        /// <summary>
        /// ����������� ������, ������� ��� � ��� ���
        /// ���������� �������������
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void Release(T obj)
        {
            if (obj == null)
                throw new NullReferenceException();
            lock (_pool)
            {
                var refThis = new WeakReference(obj);
                _pool.Add(refThis);
                _semaphore.Release();
            }
        }
    }
}
