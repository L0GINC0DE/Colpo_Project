using System;
using System.Collections.Generic;

public class LDY_MinHeap<T>
{
    private readonly List<(T item, float priority)> heap = new List<(T item, float priority)>();

    public int Count => heap.Count;
    public bool IsEmpty => heap.Count == 0;

    // 힙의 맨 끝(마지막 리프)에 새 항목을 추가한 뒤,
    // 부모보다 우선순위가 작을 동안 계속 자리를 바꿔가며 위로 올려보낸다 (sift-up).
    public void Push(T item, float priority)
    {
        heap.Add((item, priority));
        int i = heap.Count - 1;

        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].priority <= heap[i].priority)
                break; // 부모가 더 작거나 같으면 힙 조건을 만족하므로 종료

            (heap[parent], heap[i]) = (heap[i], heap[parent]);
            i = parent;
        }
    }

    // 이후 마지막 항목을 루트 자리로 옮기고, 자식 중 더 작은 쪽과 계속 자리를 바꾸며
    // 아래로 내려보낸다 (sift-down). 이렇게 하면 트리 전체를 다시 정렬하지 않아도
    // 힙 조건(부모 <= 자식)이 유지된다.
    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("힙이 비어 있습니다.");

        T root = heap[0].item;
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        int i = 0;
        int count = heap.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < count && heap[left].priority < heap[smallest].priority)
                smallest = left;
            if (right < count && heap[right].priority < heap[smallest].priority)
                smallest = right;

            if (smallest == i)
                break; // 더 이상 내려갈 필요가 없으면 종료

            (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
            i = smallest;
        }

        return root;
    }
}
