using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovingObject : MonoBehaviour
{
    public float moveTime = 0.1f;
    public LayerMask blockingLayer;

    private Rigidbody2D rb2D;
    private BoxCollider2D boxCollider;
    private float inverseMoveTime;
	// Use this for initialization
	protected virtual void Start () //可被继承类重写，protected-可被继承类获取，virtual-可被继承类重写
	{
	    rb2D = GetComponent<Rigidbody2D>();
	    boxCollider = GetComponent<BoxCollider2D>();
	    inverseMoveTime = 1f / moveTime;
	}

    protected bool Move(int xDir, int yDir, out RaycastHit2D hit)
    {
        Vector2 start = transform.position;
        Vector2 end = start + new Vector2(xDir, yDir);
        boxCollider.enabled = false;//保证不会hit到自己
        hit = Physics2D.Linecast(start, end, blockingLayer);//在blockingLayer上从start向end投射，返回射线检测到的第一个物体
        boxCollider.enabled = true;
        if (hit.transform == null)//没有物体
        {
            StartCoroutine(SmoothMovement(end));
            return true;
        }
        return false;
    }

    protected virtual void AttemptMove<T>(int xDir, int yDir) where T : Component//T是泛型，函数调用时被替换成具体类型，T指代碰撞到的component类型
    {
        RaycastHit2D hit;
        bool canMove = Move(xDir, yDir, out hit);//从Move函数中获取是否可移动，hit也被获取
        if (hit.transform == null)//如果没有障碍，就不需要这个函数，直接返回
            return;
        T hitComponent = hit.transform.GetComponent<T>();//获取到障碍物的component
        if (!canMove && hitComponent != null)
            OnCantMove(hitComponent);//将碰撞到的component传入OnCantMove函数，而OnCantMove函数将在继承类中被具体化
    }

    protected IEnumerator SmoothMovement(Vector3 end)//end是移动的目的地
    {
        float sqrRemainDistance = (transform.position - end).sqrMagnitude;//Magnitude是向量的长度，sqrMagnitude是向量长度的平方
        while (sqrRemainDistance > float.Epsilon)//float.Epsilon表示无限接近于0的float，因为float无法直接跟0比较，所以用float.Epsilon替代
        {
            Vector3 newPosition = Vector3.MoveTowards(rb2D.position, end, inverseMoveTime*Time.deltaTime);
            rb2D.MovePosition(newPosition);
            sqrRemainDistance = (transform.position - end).sqrMagnitude;//更新距离
            yield return null;//暂停协同程序，下一帧再继续往下执行
        }
    }

    protected abstract void OnCantMove<T>(T component) where T : Component;
}
