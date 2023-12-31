using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    //Moving
    private float horizontal;
    public float speed = 6f;
    public float maxFallingSpeed = -5.0f;
    public float jumpingPower = 12f;
    private bool isFacingRight = true;

    //Dashing
    public bool canDash = true;
    private bool isDashing;
    private float dashingPower = 18f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 0.5f;

    //Wall Sliding & Jumping
    private bool isWallSliding;
    private float wallSlidingSpeed = 2f;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.2f;
    private Vector2 wallJumpingPower = new Vector2(8f, 14f);
    private float wallClimbSpeed = 5f;
    private bool hasJumped = false;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    public Animator animator;
    private PolygonCollider2D[] colliders;
    public bool aliveTrig = true;
    public bool alive = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            GameObject oldPlayer = GameObject.Find("Player");
            if(SceneManager.GetActiveScene().name != "Main Menu")
            {
                GameObject.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>().Follow = oldPlayer.transform;
            }
            return;
        }
        Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {

        if (isDashing || !alive)
        {
            if (!alive)
            {
                rb.velocity = Vector2.zero;
            }
            
            return;
        }


        horizontal = Input.GetAxisRaw("Horizontal");

        if(IsGrounded())
        {
            hasJumped = false;
            animator.SetBool("IsGround", true);
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            if(Mathf.Abs(horizontal) == 1)
            {
                FindObjectOfType<AudioManager>().Play("SlimeWalk");
            }
            animator.SetBool("IsJumping", false);
        }
        else if(!IsGrounded() && !IsWalled())
        {
            animator.SetBool("IsGround", false);
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsJumping", true);
        }

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            animator.SetBool("IsGround", false);
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            animator.SetBool("IsJumping", true);
            if(!hasJumped)
            {
                FindObjectOfType<AudioManager>().Play("SlimeJump");
                hasJumped = true;
            }
        }

        //hold for higher jump
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        if(Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        if(IsGrounded() || IsWalled())
        {
            canDash = true;
        }

        if(aliveTrig == false)
        {
            
            StartCoroutine(DeathAnim());
        }

        //Debug.DrawRay(groundCheck.position, Vector2.down * 0.2f, Color.red); //Ground Checker for Visualization

        colliders = gameObject.GetComponents<PolygonCollider2D>();
        WallClimb();
        WallSlide();
        WallJump();
        if (!isWallJumping)
        {
            Flip();
        }
        
    }

    private void FixedUpdate()
    {
        if (isDashing || !alive)
        {
            if (!alive)
            {
                rb.velocity = Vector2.zero;
            }
            return;

        }

        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
        }

        if (rb.velocity.y < maxFallingSpeed)
        {
            rb.AddForce(new Vector2(0f, Physics2D.gravity.y * maxFallingSpeed));
        }
        //Debug.Log(rb.velocity.y);

    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheck.position, new Vector2(0.7f, 0.1f), 0, groundLayer);
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapBox(wallCheck.position, new Vector2(0.1f, 0.3f), 0, wallLayer);
        //return Physics2D.OverlapCircle(wallCheck.position, 0.1f, wallLayer);
    }
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        //Gizmos.DrawWireSphere(wallCheck.position, 0.1f);
        Gizmos.DrawWireCube(groundCheck.position, new Vector2(0.7f, 0.1f));
        Gizmos.DrawWireCube(wallCheck.position, new Vector2(0.1f, 0.3f));
    }
    private void WallSlide()
    {
        //if (IsWalled() && !IsGrounded() && horizontal != 0f)
        if (IsWalled() && !IsGrounded())
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsWalled", true);
            
            //colliders[0].enabled = false;
            //colliders[1].enabled = true;

            isWallSliding = true;
            if(horizontal == 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
            }
            
        }
        else
        {
            animator.SetBool("IsWalled", false);
            isWallSliding = false;
            //colliders[0].enabled = true;
            //colliders[1].enabled = false;
        }
    }

    private void WallClimb()
    {
        if (IsWalled() && !IsGrounded())
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsWalled", true);

            isWallSliding = true;
            if (Input.GetKey(KeyCode.W) && horizontal != 0)
            {
                rb.velocity = new Vector2(0, Mathf.Clamp(rb.velocity.y, wallClimbSpeed, float.MaxValue));
            }
            else if(Input.GetKey(KeyCode.S) && horizontal != 0)
            {
                //Debug.Log(rb.velocity);
                rb.velocity = new Vector2(0, -wallClimbSpeed);
            }
            
        }
        else
        {
            animator.SetBool("IsWalled", false);
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if(transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Dash()
    {
        if (!canDash) yield break;
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
    }

    private IEnumerator DeathAnim()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
        alive = false;
        animator.SetBool("Dead", true);
        
        yield return new WaitForSeconds(0.6f);
        animator.SetBool("Dead", false);
        yield return new WaitForSeconds(0.6f);
        alive = true;
    }
}
