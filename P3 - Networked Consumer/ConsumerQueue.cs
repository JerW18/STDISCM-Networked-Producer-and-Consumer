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
        private static int queueCount = 0;
        private static readonly object countLock = new();
        public enum EnqueueStatus
        {
            Success,
            Duplicate,
            Full
        }

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

        public static EnqueueStatus TryEnqueue(string fileName, byte[] fileData, byte[] hash)
        {
            string hashString = Convert.ToHexString(hash);

            if (IsDuplicate(hash))
                return EnqueueStatus.Duplicate;

            if (!queueLock.WaitOne(0))
            {
                Logger.Log($"[Queue] Rejected (FULL): {fileName}");
                return EnqueueStatus.Full;
            }

            uploadQueue.Enqueue(new VideoFileItem
            {
                FileName = fileName,
                FileData = fileData,
                Hash = hashString
            });

            lock (videoHashes)
                videoHashes.Add(hashString);

            Logger.Log($"[Queue] Enqueued: {fileName} — Queue size: {uploadQueue.Count}");
            return EnqueueStatus.Success;
        }

        public static bool GetSlot()
        {
            return queueLock.WaitOne(0);
        }

        public static void FinishEnqueue(string fileName, byte[] fileData, byte[] hash)
        {
            string hashString = Convert.ToHexString(hash);

            uploadQueue.Enqueue(new VideoFileItem
            {
                FileName = fileName,
                FileData = fileData,
                Hash = hashString
            });

            lock (videoHashes)
                videoHashes.Add(hashString);

            Logger.Log($"[Queue] Enqueued: {fileName} — Queue size: {uploadQueue.Count}");
        }

        public static bool TryDequeue(out VideoFileItem? item)
        {
            bool success = uploadQueue.TryDequeue(out item);
            if (success)
            {
                Logger.Log($"[Queue] Dequeued: {item.FileName} — Queue size: {uploadQueue.Count}");
                queueLock.Release();
                lock (countLock)
                    queueCount--;
            }

            return success;
        }

        public static bool TryRequeue(VideoFileItem item)
        {
            bool acquiredSemaphore = false;
            try
            {
                if (!queueLock.WaitOne(0))
                {
                    Logger.Log($"[Queue] Failed to Requeue (FULL): {item.FileName}");
                    return false;
                }
                acquiredSemaphore = true;

                uploadQueue.Enqueue(item);
                Logger.Log($"[Queue] Requeued: {item.FileName} — Queue size: {uploadQueue.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Queue] EXCEPTION during Requeue for {item.FileName}: {ex}");
                if (acquiredSemaphore)
                {
                    queueLock.Release();
                }
                return false; 
            }
        }

        public static int CurrentQueueCount => uploadQueue.Count;
    }
}