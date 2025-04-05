using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading;
using P3___Networked_Consumer.Models;

namespace P3___Networked_Consumer
{
    public static class ConsumerQueue
    {
        private const int queueLength = 10;
        private static ConcurrentQueue<VideoFileItem> uploadQueue = new();
        private static HashSet<string> videoHashes = new HashSet<string>();
        private static Semaphore queueLock = new(queueLength, queueLength);

        public static void InitializeQueue(int maxLength)
        {
            uploadQueue = new ConcurrentQueue<VideoFileItem>();
            videoHashes = new HashSet<string>();
            queueLock = new Semaphore(maxLength, maxLength);
        }

        public static bool IsDuplicate(byte[] hash)
        {
            string hashString = Convert.ToHexString(hash);
            lock (videoHashes)
            {
                //Logger.Log($"[Queue] Duplicate check for hash {hashString}: {videoHashes.Contains(hashString)}");
                Logger.Log($"[Queue] Duplicate check for hash {hashString}: {videoHashes.Contains(hashString)}");
                return videoHashes.Contains(hashString);
            }
        }

        public static bool TryEnqueue(string fileName, byte[]fileData, byte[] hash)
        {
            string hashString = Convert.ToHexString(hash);

            if (IsDuplicate(hash))
            {
                return false;
            }

            queueLock.WaitOne(); //block producer if full

            uploadQueue.Enqueue(new VideoFileItem
            {
                FileName = fileName,
                FileData = fileData,
                Hash = hashString
            });

            //Logger.Log($"[Queue] Enqueued: {fileName} (Hash: {hashString}) — Queue size: {uploadQueue.Count}");
            Logger.Log($"[Queue] Enqueued: {fileName} — Queue size: {uploadQueue.Count}");

            lock (videoHashes)
            {
                videoHashes.Add(hashString);
            }

            return true;
        }

        public static bool TryDequeue(out VideoFileItem? item)
        {
            bool success = uploadQueue.TryDequeue(out item);
            if (success)
            {
                Logger.Log($"[Queue] Dequeued: {item.FileName} — Queue size: {uploadQueue.Count}");
                queueLock.Release();
            }

            return success;
        }

        public static void Requeue(VideoFileItem item)
        {
            uploadQueue.Enqueue(item);
            Logger.Log($"[Queue] Requeued: {item.FileName} — Queue size: {uploadQueue.Count}");
        }

        public static int CurrentQueueCount => uploadQueue.Count;
    }
}