using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    // PUBLIC
    // Walk
    public float maxWalkSpeed = 8;
    public float walkAcceleration = 6;
    public float maxSlopeAngle = 60;
   
    // Jump
    public int jumpMaxCount = 2;
    public float jumpMaxHeight = 4;
    public float jumpTimeToMaxHeight = 0.5f;
    public bool allowDoubleJumpWhenFalling = false;

    // Probing
    public float probingMaxDistance = 5;
    public float groundOffsetY = 0.01f;
    public float groundOnSlopeOffsetY = 0.01f;
    public float groundOffsetX = 0.01f;
    public float raySideOffsetX = 0.05f;
    public float raySideOffsetY = 0.05f;
    public float rayBottomOffsetX = 0.1f;
    public float rayBottomOffsetY = 0.5f;

    // PRIVATE
    // Walk
    private float currentSpeedX;
    private float currentSpeedY;
    private float currentMaxSpeedX;

    // Jump
    private float jumpInitialVelocity;
    private int jumpCount = 0;
    private float gravity;

    // Movement states
    private Vector2 amountTomve;
    private bool isMoving = false;
    private bool isJumpButtonPressed = false;
    private bool isJumping = false;
    private float movementDir;

    // Probing
    private Vector3 bottomLeftCorner;
    private Vector3 bottomRightCorner;
    private Vector3 bottomCenterCorner;
    private Vector3 topLeftCorner;
    private Vector3 topRightCorner;
    private Vector3 topCenterCorner;
    private Vector3 sideTopCorner;
    private Vector3 sideBottomCorner;
    private Vector3 sideCenterCorner;
    private Vector3 colliderSize;

    private bool isOnSlope = false;
    private bool isOnSlopeL = false;
    private bool isOnSlopeR = false;
    private bool isOnSlopeC = false;
    private bool isOnNonWalkableSlope = false;
    private bool isOnNonWalkableSlopeL = false;
    private bool isOnNonWalkableSlopeR = false;
    private bool isOnNonWalkableSlopeC = false;

    private bool isGrounded = false;
    private bool isGroundedL = false;
    private bool isGroundedR = false;
    private bool isGroundedC = false;

    private bool isBumpedTop = false;
    private bool isBumpedTopL = false;
    private bool isBumpedTopR = false;
    private bool isBumpedTopC = false;

    private bool isBumpedSide = false;
    private bool isBumpedSideT = false;
    private bool isBumpedSideB = false;
    private bool isBumpedSideC = false;

    // Debugging
    private float debugLineLifetime = 0.01f;



    // Start is called before the first frame update
    void Start()
    {
        colliderSize = GetComponent<Collider>().bounds.size;
        gravity = 2 * jumpMaxHeight / (jumpTimeToMaxHeight * jumpTimeToMaxHeight);
        jumpInitialVelocity = 2 * jumpMaxHeight / jumpTimeToMaxHeight;

    }


    // Update is called once per frame
    void Update()
    {
        isJumpButtonPressed = Input.GetButtonDown("Jump");

        currentMaxSpeedX = Input.GetAxisRaw("Horizontal") * maxWalkSpeed;
        currentSpeedX = GetCurrentSpeedX(currentSpeedX, currentMaxSpeedX, walkAcceleration);
        currentSpeedY = GetCurrentSpeedY(currentSpeedY, jumpInitialVelocity, gravity);

        Vector2 moveVector = new Vector2();
        moveVector.x = currentSpeedX * Time.deltaTime;
        moveVector.y = currentSpeedY * Time.deltaTime;

        isMoving = moveVector.x != 0;

        UpdateCornerPosition(moveVector);
        TryToMove(moveVector);
    }

    public float GetMovementDir() {
        return movementDir;
    }

    private float GetCurrentSpeedX(float speed, float maxSpeed, float acceleration)
    {
        movementDir = (Mathf.Abs(maxSpeed) > 0)? Mathf.Sign(maxSpeed - speed):0;

        if (speed == maxSpeed) {
            return speed;

        } else {
            speed += acceleration * Time.deltaTime * movementDir;

            return (movementDir == Mathf.Sign(maxSpeed - speed)) ? speed : maxSpeed;
        }
    }

    private float GetCurrentSpeedY(float speed, float jumpInitialVelocity, float gravity)
    {
        if (isJumpButtonPressed && jumpCount < jumpMaxCount) {
            isJumping = true;
            speed = jumpInitialVelocity + gravity * Time.deltaTime * -1;
            jumpCount++;

        } else if (isGrounded) {
            speed = 0;
            jumpCount = 0;
            isJumping = false;

        } else {
            // Prevents double jump
            if (!isJumping && jumpCount == 0 && !allowDoubleJumpWhenFalling) {
                jumpCount = 1;
            }

            speed = speed + gravity * Time.deltaTime * -1;
        }

        return speed;
    }

    private void TryToMove(Vector2 moveVector)
    {
        moveVector = ProbeDownward(moveVector);
        moveVector = ProbeSides(moveVector);
        moveVector = ProbeUpward(moveVector);

        transform.position = transform.position + new Vector3(moveVector.x, moveVector.y, 0);
    }

    private Vector3 ProbeDownward(Vector3 moveVector)
    {
        Vector3 nextBottomPosition = bottomCenterCorner + moveVector - new Vector3(0, rayBottomOffsetY, 0);
        float bottomPositionL = 0;
        float bottomPositionR = 0;
        float bottomPositionC = 0;

        RaycastHit leftHit;
        if (Physics.Raycast(bottomLeftCorner + moveVector, transform.TransformDirection(Vector3.down), out leftHit, probingMaxDistance)) {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * leftHit.distance, Color.red, debugLineLifetime);

            float leftHitAngle = Vector2.Angle(Vector2.up, new Vector2(leftHit.normal.x, leftHit.normal.y));

            Debug.Log("slope leftHitAngle = " + leftHitAngle);

            if (leftHitAngle > 0) {
                isOnSlopeL = true;
                Debug.Log("slope leftHitAngle >0 = " + leftHitAngle);
                if (leftHitAngle > maxSlopeAngle) {
                    Debug.Log("slope leftHitAngle >0 && >max = " + leftHitAngle);
                    isOnNonWalkableSlopeL = true;
                } else {
                    isOnNonWalkableSlopeL = false;
                }
            } else {
                isOnSlopeL = false;
                isOnNonWalkableSlopeL = false;
            }

            bottomPositionL = leftHit.point.y + groundOffsetY;
            if (leftHit.point.y + groundOffsetY > nextBottomPosition.y) {
                isGroundedL = true;

            } else {
                isGroundedL = false;
                //isOnNonWalkableSlopeL = false;
                //isOnSlopeL = false;
            }
        } else {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedL = false;
        }

        RaycastHit rightHit;
        if (Physics.Raycast(bottomRightCorner + moveVector, transform.TransformDirection(Vector3.down), out rightHit, probingMaxDistance)) {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * rightHit.distance, Color.red, debugLineLifetime);

            float rightHitAngle = Vector2.Angle(Vector2.up, new Vector2(rightHit.normal.x, rightHit.normal.y));

            Debug.Log("slope rightHitAngle = " + rightHitAngle);

            if (rightHitAngle > 0) {
                isOnSlopeR = true;
                Debug.Log("slope leftHitAngle >0 = " + rightHitAngle);
                if (rightHitAngle > maxSlopeAngle) {
                    Debug.Log("slope leftHitAngle >0 && >max =  " + rightHitAngle);
                    isOnNonWalkableSlopeR = true;
                } else {
                    isOnNonWalkableSlopeR = false;
                }
            } else {
                isOnSlopeR = false;
                isOnNonWalkableSlopeR = false;
            }

            bottomPositionR = rightHit.point.y + groundOffsetY;
            if (rightHit.point.y + groundOffsetY > nextBottomPosition.y) {
                isGroundedR = true;

            } else {
                isGroundedR = false;
                //isOnNonWalkableSlopeR = false;
                //isOnSlopeR = false;
            }
        } else  {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedR = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(bottomCenterCorner + moveVector, transform.TransformDirection(Vector3.down), out centerHit, probingMaxDistance)) {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * centerHit.distance, Color.red, debugLineLifetime);

            float centerHitAngle = Vector2.Angle(Vector2.up, new Vector2(centerHit.normal.x, centerHit.normal.y));

            Debug.Log("slope centerHitAngle = " + centerHitAngle);

            if (centerHitAngle > 0) {
                isOnSlopeC = true;
                Debug.Log("slope leftHitAngle >0 = " + centerHitAngle);
                if (centerHitAngle > maxSlopeAngle) {
                    Debug.Log("slope leftHitAngle >0 && >max =  " + centerHitAngle);
                    isOnNonWalkableSlopeC = true;
                } else {
                    isOnNonWalkableSlopeC = false;
                }
            } else {
                isOnSlopeC = false;
                isOnNonWalkableSlopeC = false;
            }

            bottomPositionC = centerHit.point.y + groundOffsetY;
            if (centerHit.point.y + groundOffsetY > nextBottomPosition.y) {
                isGroundedC = true;

            } else {
                isGroundedC = false;
                //isOnNonWalkableSlopeC = false;
                //isOnSlopeC = false;
            }
        } else  {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedC = false;
        }

        isGrounded = isGroundedL || isGroundedR || isGroundedC;
        isOnNonWalkableSlope = isOnNonWalkableSlopeL || isOnNonWalkableSlopeR || isOnNonWalkableSlopeC;

        if (isOnNonWalkableSlope) {
            Debug.Log("UnWalkableSlope");

        } else {
            isOnSlope = (isOnSlopeR && !isOnNonWalkableSlopeR) || (isOnSlopeL && !isOnNonWalkableSlopeL);
            if (isOnSlope) {
                nextBottomPosition.y = Mathf.Max(bottomPositionL, bottomPositionR, bottomPositionC);
                moveVector.y = nextBottomPosition.y - bottomCenterCorner.y + rayBottomOffsetY;// + ((isOnSlopeR || isOnSlopeL)?groundOnSlopeOffsetY:0);
            } else if(isGrounded) {
                moveVector.y = nextBottomPosition.y - bottomCenterCorner.y + rayBottomOffsetY;// + ((isOnSlopeR || isOnSlopeL)?groundOnSlopeOffsetY:0);
            } else {
                Debug.Log("!!ojapio isOnSlope = " + isOnSlope + " isGrounded = " + isGrounded );
            }
        }

        return moveVector;
    }

    private Vector3 ProbeUpward(Vector3 moveVector)
    {
        Vector3 nextTopPosition = topCenterCorner + moveVector + new Vector3(0, rayBottomOffsetY, 0);

        if (currentSpeedY > 0) {
            RaycastHit leftHit;
            if (Physics.Raycast(topLeftCorner, transform.TransformDirection(Vector3.up), out leftHit, probingMaxDistance)) {
                Debug.DrawRay(topLeftCorner, transform.TransformDirection(Vector3.up) * leftHit.distance, Color.red, debugLineLifetime);


                if (leftHit.point.y - groundOffsetY < nextTopPosition.y && leftHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
                    nextTopPosition.y = leftHit.point.y - groundOffsetY;
                    isBumpedTopL = true;

                } else {
                    isBumpedTopL = false;
                }
            } else {
                Debug.DrawRay(topLeftCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedTopL = false;
            }

            RaycastHit rightHit;
            if (Physics.Raycast(topRightCorner, transform.TransformDirection(Vector3.up), out rightHit, probingMaxDistance)) {
                Debug.DrawRay(topRightCorner, transform.TransformDirection(Vector3.up) * rightHit.distance, Color.red, debugLineLifetime);

                if (rightHit.point.y - groundOffsetY < nextTopPosition.y  && rightHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
                    nextTopPosition.y = rightHit.point.y - groundOffsetY;
                    isBumpedTopR = true;

                } else {
                    isBumpedTopR = false;
                }
            } else  {
                Debug.DrawRay(topRightCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedTopR = false;
            }

            RaycastHit centerHit;
            if (Physics.Raycast(topCenterCorner, transform.TransformDirection(Vector3.up), out centerHit, probingMaxDistance)) {
                Debug.DrawRay(topCenterCorner, transform.TransformDirection(Vector3.up) * centerHit.distance, Color.red, debugLineLifetime);

                if (centerHit.point.y - groundOffsetY < nextTopPosition.y  && centerHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
                    nextTopPosition.y = centerHit.point.y - groundOffsetY;
                    isBumpedTopC = true;

                } else {
                    isBumpedTopC = false;
                }
            } else  {
                Debug.DrawRay(topCenterCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedTopC = false;
            }

            isBumpedTop = isBumpedTopL || isBumpedTopR || isBumpedTopC;

            if (isBumpedTop) {
                currentSpeedY = 0;
            }

            moveVector.y = nextTopPosition.y - topCenterCorner.y - rayBottomOffsetY;
            
        }
        return moveVector;
    }

    private Vector3 ProbeSides(Vector3 moveVector)
    {
        float dir = Mathf.Sign(moveVector.x);
        Vector3 nextSidePosition = sideCenterCorner + moveVector + new Vector3(raySideOffsetX, 0, 0) * dir;
        float moveSign;

        if (isMoving) {
            RaycastHit topHit;
            if (Physics.Raycast(sideTopCorner + moveVector, transform.TransformDirection(Vector3.right) * dir, out topHit, probingMaxDistance)) {
                float topHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(topHit.normal.x, topHit.normal.y)) - 90;
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * topHit.distance, Color.red, debugLineLifetime);

                moveSign = Mathf.Sign(nextSidePosition.x - topHit.point.x + dir * groundOffsetX);
                
                if (moveSign == dir) {
                    nextSidePosition.x = topHit.point.x - groundOffsetX * dir;
                    isBumpedSideT = true;

                } else {
                    isBumpedSideT = false;
                }


            } else {
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideT = false;
            }

            RaycastHit bottomHit;
            if (Physics.Raycast(sideBottomCorner + moveVector, transform.TransformDirection(Vector3.right) * dir, out bottomHit, probingMaxDistance)) {
                float bottomHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(bottomHit.normal.x, bottomHit.normal.y)) - 90;
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * bottomHit.distance, Color.red, debugLineLifetime);

                moveSign = Mathf.Sign(nextSidePosition.x - bottomHit.point.x + dir * groundOffsetX);
                
                if (moveSign == dir) {
                    nextSidePosition.x = bottomHit.point.x - groundOffsetX * dir;
                    isBumpedSideB = true;

                } else {
                    isBumpedSideB = false;
                }


            } else  {
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideB = false;
            }

            RaycastHit centerHit;
            if (Physics.Raycast(sideCenterCorner + moveVector, transform.TransformDirection(Vector3.right) * dir, out centerHit, probingMaxDistance)) {
                float centerHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(centerHit.normal.x, centerHit.normal.y)) - 90;
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * centerHit.distance, Color.red, debugLineLifetime);

                moveSign = Mathf.Sign(nextSidePosition.x - centerHit.point.x + dir * groundOffsetX);
                
                if (moveSign == dir) {
                    nextSidePosition.x = centerHit.point.x - groundOffsetX * dir;
                    isBumpedSideC = true;

                } else {
                    isBumpedSideC = false;
                }


            } else  {
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideC = false;
            }

            isBumpedSide = isBumpedSideT || isBumpedSideB || isBumpedSideC;

            if (isBumpedSide) {
                currentSpeedX = 0;
            }

            if (isOnSlope) {
                Debug.Log("!!!!");

                nextSidePosition = sideCenterCorner + moveVector + new Vector3(raySideOffsetX, 0, 0) * dir;
            }

            moveVector.x = nextSidePosition.x - sideCenterCorner.x - raySideOffsetX * dir;
            
        }
        return moveVector;
    }

    private void UpdateCornerPosition(Vector2 moveVector)
    {
        // Update 3 bottom probing points
        bottomLeftCorner = new Vector3(
            transform.position.x - colliderSize.x / 2 - rayBottomOffsetX,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );
        bottomRightCorner = new Vector3(
            transform.position.x + colliderSize.x / 2 + rayBottomOffsetX,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );
        bottomCenterCorner = new Vector3(
            transform.position.x,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );

        // Update 3 top probing points
        topLeftCorner = new Vector3(
            transform.position.x - colliderSize.x / 2 - rayBottomOffsetX,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );
        topRightCorner = new Vector3(
            transform.position.x + colliderSize.x / 2 + rayBottomOffsetX,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );
        topCenterCorner = new Vector3(
            transform.position.x,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );

        // Update 3 side probing points according to movement direction (dir)
        float dir = Mathf.Sign(moveVector.x);
        if (isMoving)
        {
            sideTopCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y + colliderSize.y / 2
            );
            sideBottomCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y - colliderSize.y / 2
            );
            sideCenterCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y
            );
        }
    }
}
