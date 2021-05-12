using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    //Player handling
    public float maxWalkSpeed = 8;
    public float walkAcceleration = 2;
    public float gravity = 2;
    public float groundOffsetY = 0.01f;
    public float groundOffsetX = 0.01f;
    public float probingMaxDistance = 1;

    public jumpMaxHeight;
    public jumpTimeToMaxHeight;

    private float currentSpeedX;
    private float currentSpeedY;
    private float currentMaxSpeedX;
    private Vector2 amountTomve;
    private Vector3 bottomLeftCorner;
    private Vector3 bottomRightCorner;
    private Vector3 bottomCenterCorner;
    private Vector3 sideTopCorner;
    private Vector3 sideBottomCorner;
    private Vector3 sideCenterCorner;
    private Vector3 colliderSize;
    private float groundPositionY;
    private bool isMoving = false;
    private bool isJumping = false;

    private bool isGrounded = false;
    private bool isGroundedL = false;
    private bool isGroundedR = false;
    private bool isGroundedC = false;

    private bool isBumped = false;
    private bool isBumpedT = false;
    private bool isBumpedB = false;
    private bool isBumpedC = false;



    // Start is called before the first frame update
    void Start()
    {
        colliderSize = GetComponent<Collider>().bounds.size;
    }


    // Update is called once per frame
    void Update()
    {

        currentMaxSpeedX = Input.GetAxisRaw("Horizontal") * maxWalkSpeed;
        isJumping = Input.GetAxisRaw("Jump");
        currentSpeedX = GetCurrentSpeed(currentSpeedX, currentMaxSpeedX, walkAcceleration);
        currentSpeedY = currentSpeedY + gravity * Time.deltaTime * -1;

        Vector2 moveVector = new Vector2();
        moveVector.x = currentSpeedX * Time.deltaTime;

        isMoving = moveVector.x != 0 && Mathf.Abs(moveVector.x) > 0;
        if (!isGrounded) {
            moveVector.y = currentSpeedY * Time.deltaTime;
        }

        UpdateCornerPosition(moveVector);
        TryToMove(moveVector);
    }

    private float GetCurrentSpeed(float speed, float maxSpeed, float acceleration)
    {
        if (speed == maxSpeed) {
            return speed;

        } else {
            float dir = Mathf.Sign(maxSpeed - speed);
            speed += acceleration * Time.deltaTime * dir;

            return (dir == Mathf.Sign(maxSpeed - speed)) ? speed : maxSpeed;
        }
    }

    private void TryToMove(Vector2 moveVector)
    {
        moveVector = ProbeSides(moveVector);
        moveVector = ProbeDownward(moveVector);

        transform.Translate(moveVector);
    }

    private Vector3 ProbeDownward(Vector3 moveVector)
    {
        Vector3 nextBottomPosition = bottomCenterCorner + moveVector;

        RaycastHit leftHit;
        if (Physics.Raycast(bottomLeftCorner, transform.TransformDirection(Vector3.down), out leftHit, probingMaxDistance)) {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down), Color.red, leftHit.distance);

            if (leftHit.point.y > nextBottomPosition.y) {
                nextBottomPosition.y = leftHit.point.y + groundOffsetY;
                isGroundedL = true;

            } else {
                isGroundedL = false;
            }
        } else {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down), Color.white, probingMaxDistance);
            isGroundedL = false;
        }

        RaycastHit rightHit;
        if (Physics.Raycast(bottomRightCorner, transform.TransformDirection(Vector3.down), out rightHit, probingMaxDistance)) {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down), Color.red, rightHit.distance);

            if (rightHit.point.y > nextBottomPosition.y) {
                nextBottomPosition.y = rightHit.point.y + groundOffsetY;
                isGroundedR = true;

            } else {
                isGroundedR = false;
            }
        } else  {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down), Color.white, probingMaxDistance);
            isGroundedR = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(bottomCenterCorner, transform.TransformDirection(Vector3.down), out centerHit, probingMaxDistance)) {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down), Color.red, centerHit.distance);

            if (centerHit.point.y > nextBottomPosition.y) {
                nextBottomPosition.y = centerHit.point.y + groundOffsetY;
                isGroundedC = true;

            } else {
                isGroundedC = false;
            }
        } else  {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down), Color.white, probingMaxDistance);
            isGroundedC = false;
        }

        isGrounded = isGroundedL || isGroundedR || isGroundedC;

        moveVector.y = nextBottomPosition.y - bottomCenterCorner.y;

        return moveVector;
    }

    private Vector3 ProbeSides(Vector3 moveVector)
    {
        Vector3 nextSidePosition = sideCenterCorner + moveVector;
        float dir = Mathf.Sign(moveVector.x);
        float moveSign;

        if (isMoving) {
            RaycastHit topHit;

            if (Physics.Raycast(sideTopCorner, transform.TransformDirection(Vector3.right) * dir, out topHit, probingMaxDistance)) {
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir, Color.red, topHit.distance);

                moveSign = Mathf.Sign(nextSidePosition.x - topHit.point.x);
                Debug.Log("moveSign = " + moveSign + " topHit.point.x = " + topHit.point.x + "nextSidePosition.x = " + nextSidePosition.x);
                if (moveSign == dir) {
                    Debug.Log("wants to move to = t " + nextSidePosition + " but sShould stop at = " + topHit.point.x);
                    nextSidePosition.x = topHit.point.x - groundOffsetX * dir;
                    isBumpedT = true;

                } else {
                    isBumpedT = false;
                }
            } else {
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir, Color.white, probingMaxDistance);
                isBumpedT = false;
            }

            RaycastHit bottomHit;
            if (Physics.Raycast(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir, out bottomHit, probingMaxDistance)) {
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir, Color.red, bottomHit.distance);

                moveSign = Mathf.Sign(nextSidePosition.x - bottomHit.point.x);
                Debug.Log("moveSign = " + moveSign + " bottomHit.point.x = " + bottomHit.point.x + "nextSidePosition.x = " + nextSidePosition.x);
                if (moveSign == dir) {
                    Debug.Log("wants to move to = " + nextSidePosition + " but should stop at = " + bottomHit.point.x);
                    nextSidePosition.x = bottomHit.point.x - groundOffsetX * dir;
                    isBumpedB = true;

                } else {
                    isBumpedB = false;
                }
            } else  {
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir, Color.white, probingMaxDistance);
                isBumpedB = false;
            }

            RaycastHit centerHit;
            if (Physics.Raycast(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir, out centerHit, probingMaxDistance)) {
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir, Color.red, centerHit.distance);

                moveSign = Mathf.Sign(nextSidePosition.x - centerHit.point.x);
                Debug.Log("moveSign = " + moveSign + " centerHit.point.x = " + centerHit.point.x + "nextSidePosition.x = " + nextSidePosition.x);
                if (moveSign == dir) {
                    Debug.Log("wants to move to = " + nextSidePosition + " but should stop at = " + centerHit.point.x);
                    nextSidePosition.x = centerHit.point.x - groundOffsetX * dir;
                    isBumpedC = true;

                } else {
                    isBumpedC = false;
                }
            } else  {
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir, Color.white, probingMaxDistance);
                isBumpedC = false;
            }

            isBumped = isBumpedT || isBumpedB || isBumpedC;

            if (isBumped) {
                currentSpeedX = 0;
            }

            moveVector.x = nextSidePosition.x - sideCenterCorner.x;
        }
        return moveVector;
    }

    private void UpdateCornerPosition(Vector2 moveVector)
    {
        bottomLeftCorner = new Vector3(
            transform.position.x - colliderSize.x / 2,
            transform.position.y - colliderSize.y / 2
        );
        bottomRightCorner = new Vector3(
            transform.position.x + colliderSize.x / 2,
            transform.position.y - colliderSize.y / 2
        );
        bottomCenterCorner = new Vector3(
            transform.position.x,
            transform.position.y - colliderSize.y / 2
        );

        float dir = Mathf.Sign(moveVector.x);
        if (isMoving)
        {
            sideTopCorner = new Vector3(
                transform.position.x + colliderSize.x / 2 * dir,
                transform.position.y + colliderSize.y / 2
            );
            sideBottomCorner = new Vector3(
                transform.position.x + colliderSize.x / 2 * dir,
                transform.position.y - colliderSize.y / 2 + 0.1f
            );
            sideCenterCorner = new Vector3(
                transform.position.x + colliderSize.x / 2 * dir,
                transform.position.y
            );
        }
    }
}
