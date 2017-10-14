using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MovingObject
{
    public int wallDamage = 1;
    public int pointsPerFood = 10;
    public int pointsPerSoda = 20;
    public float restartLevelDelay = 1f;
    public Text foodText;
    public AudioClip moveSound1;
    public AudioClip moveSound2;
    public AudioClip eatSound1;
    public AudioClip eatSound2;
    public AudioClip drinkSound1;
    public AudioClip drinkSound2;
    public AudioClip gameOverSound;

    private Animator animator;
    private int food;
    private Vector2 touchOrigin = -Vector2.one;

	protected override void Start ()
	{
	    animator = GetComponent<Animator>();
	    food = GameManager.instance.playerFoodPoints;
	    foodText.text = "Food: " + food;
        base.Start();
	}

    private void OnDisable()
    {
        GameManager.instance.playerFoodPoints = food;
    }
	
	// Update is called once per frame
	void Update ()
	{
	    if (!GameManager.instance.playerTurn)
	        return;
	    int horizontal = 0;
	    int vertical = 0;

#if UNITY_STANDALONE || UNITY_WEBPLAYER

	    horizontal = (int) Input.GetAxisRaw("Horizontal");
	    vertical = (int) Input.GetAxisRaw("Vertical");
	    if (horizontal != 0)
	        vertical = 0;//保证只会沿着一个方向走，不会斜着走

#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE

	    if (Input.touchCount > 0)//有触摸
	    {
	        Touch myTouch = Input.touches[0];//获取检测到的首个touch
	        if (myTouch.phase == TouchPhase.Began)//如果这个touch是began阶段，即刚接触屏幕
	        {
	            touchOrigin = myTouch.position;//将这个起点touch的位置存入touchOrigin
	        }
            else if (myTouch.phase == TouchPhase.Ended && touchOrigin.x > 0)//如果这个touch是end阶段并且有触摸，即运行过上面的if体
            {
                Vector2 touchEnd = myTouch.position;//获取这个终点位置存入touchEnd
                float x = touchEnd.x - touchOrigin.x;//计算手指在x轴的滑动位移
                float y = touchEnd.y - touchOrigin.y;//计算手指在y轴的滑动位移
                touchOrigin.x = -1;
                if (Mathf.Abs(x) > Mathf.Abs(y))//如果x轴位移大于y轴，则沿x轴移动
                    horizontal = x > 0 ? 1 : -1;
                else                            //如果y轴位移大于x轴，则沿y轴移动
                    vertical = y > 0 ? 1 : -1;
            }
	    }

#endif

	    if (horizontal != 0 || vertical != 0)
	        AttemptMove<Wall>(horizontal, vertical);
	}

    protected override void AttemptMove<T>(int xDir, int yDir)
    {
        food--;
        foodText.text = "Food: " + food;
        base.AttemptMove<T>(xDir, yDir);
        RaycastHit2D hit;
        if (Move(xDir, yDir, out hit))
        {
            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
        }
        CheckIfGameOver();
        GameManager.instance.playerTurn = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Exit")
        {
            Invoke("Restart", restartLevelDelay);
            enabled =false;
        }
        else if (other.tag == "Food")
        {
            food += pointsPerFood;
            foodText.text = "+" + pointsPerFood + " Food: " + food;
            SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);
            other.gameObject.SetActive(false);
        }
        else if (other.tag == "Soda")
        {
            food += pointsPerSoda;
            foodText.text = "+" + pointsPerSoda + " Food: " + food;
            SoundManager.instance.RandomizeSfx(drinkSound1, drinkSound2);
            other.gameObject.SetActive(false);
        }
    }

    protected override void OnCantMove<T>(T component)
    {
        Wall hitWall = component as Wall;//将传入的参数component(墙的script)转换为Wall类变量hitWall
        hitWall.DamageWall(wallDamage);
        animator.SetTrigger("playerChop");
    }

    private void Restart()
    {
        SceneManager.LoadScene(0);
    }

    public void LoseFood(int loss)
    {
        animator.SetTrigger("playerHit");
        food -= loss;
        foodText.text = "-" + loss + " Food: " + food;
        CheckIfGameOver();
    }

    private void CheckIfGameOver()
    {
        if (food <= 0)
        {
            SoundManager.instance.PlaySingle(gameOverSound);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver();
        }           
    }
}
