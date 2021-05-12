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
    public float groundSlopeOffsetY = 0.05f;
    public float groundOffsetX = 0.01f;
    public float raySideOffsetX = 0.05f;
    public float raySideOffsetY = 0.05f;
    public float rayBottomOffsetX = 0.005f;
    public float rayBottomOffsetY = 0.3f;

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
    private bool isRunning = false;
    private bool isJumpButtonPressed = false;
    private bool isJumping = false;
    private float inputDir;
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
    private bool isAboveSlope = false;
    private bool isAboveSlopeL = false;
    private bool isAboveSlopeR = false;
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
        if (isJumpButtonPressed) {
            Jump();
        }

        currentMaxSpeedX = Input.GetAxisRaw("Horizontal") * maxWalkSpeed;
        currentSpeedX = GetCurrentSpeedX(currentSpeedX, currentMaxSpeedX, walkAcceleration);
        currentSpeedY = GetCurrentSpeedY(currentSpeedY, gravity);

        Vector2 moveVector = new Vector2();
        moveVector.x = currentSpeedX * Time.deltaTime;
        moveVector.y = currentSpeedY * Time.deltaTime;

        isRunning = moveVector.x != 0;
        
        UpdateCornerPosition(moveVector);
        TryToMove(moveVector);
    }

    public float GetInputDir() {
        return inputDir;
    }

    private float GetCurrentSpeedX(float speed, float maxSpeed, float acceleration)
    {
        inputDir = (Mathf.Abs(maxSpeed) > 0)? Mathf.Sign(maxSpeed - speed):0;
        movementDir = Mathf.Sign(maxSpeed - speed);

        if (speed != maxSpeed) {
            speed += acceleration * Time.deltaTime * movementDir;
            speed = (movementDir == Mathf.Sign(maxSpeed - speed)) ? speed : maxSpeed;
        }

        return speed;
    }

    private void Jump()
    {
        if (jumpCount < jumpMaxCount) {
            isJumping = true;
            isGrounded = false;
            isOnSlope = false;
            currentSpeedY = jumpInitialVelocity;
            jumpCount++;

        } else if (isGrounded) {
            jumpCount = 0;
        }
    }

    private float GetCurrentSpeedY(float speed, float gravity)
    {
        if (isGrounded && currentSpeedY <= 0.0f) {
            speed = 0.0f;

        } else {
            speed = speed - gravity * Time.deltaTime;
        }
 
        return speed;
    }

    private void TryToMove(Vector2 moveVector)
    {
        moveVector = ProbeDownward(moveVector);
        moveVector = ProbeSides(moveVector);
        moveVector = ProbeUpward(moveVector);

        if (isGrounded || isOnSlope) {
            jumpCount = 0;
            isJumping = false;

        // Prevents double jump
        } else if (currentSpeedY < 0.0f && !isJumping && jumpCount == 0 && !allowDoubleJumpWhenFalling) {
            jumpCount = 1;
        }

        transform.position = transform.position + new Vector3(moveVector.x, moveVector.y, 0.0f);
    }

    private Vector3 ProbeDownward(Vector3 moveVector)
    {
        float nextBottomPosition = bottomCenterCorner.y + moveVector.y - rayBottomOffsetY;
        float bottomPositionL = nextBottomPosition;
        float bottomPositionR = nextBottomPosition;
        float bottomPositionC = nextBottomPosition;

        RaycastHit leftHit;
        if (Physics.Raycast(bottomLeftCorner, transform.TransformDirection(Vector3.down), out leftHit, probingMaxDistance)) {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * leftHit.distance, Color.red, debugLineLifetime);

            float leftHitAngle = Vector2.Angle(Vector2.up, new Vector2(leftHit.normal.x, leftHit.normal.y));

            if (leftHitAngle > 0.0f) {
                isAboveSlopeL = true;
                if (leftHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeL = true;
                } else {
                    isOnNonWalkableSlopeL = false;
                }
            } else {
                isAboveSlopeL = false;
                isOnNonWalkableSlopeL = false;
            }

            bottomPositionL = leftHit.point.y;
            if (leftHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedL = true;

            } else {
                isGroundedL = false;
            }
        } else {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedL = false;
        }

        RaycastHit rightHit;
        if (Physics.Raycast(bottomRightCorner, transform.TransformDirection(Vector3.down), out rightHit, probingMaxDistance)) {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * rightHit.distance, Color.red, debugLineLifetime);

            float rightHitAngle = Vector2.Angle(Vector2.up, new Vector2(rightHit.normal.x, rightHit.normal.y));

            if (rightHitAngle > 0) {
                isAboveSlopeR = true;
                if (rightHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeR = true;
                } else {
                    isOnNonWalkableSlopeR = false;
                }
            } else {
                isAboveSlopeR = false;
                isOnNonWalkableSlopeR = false;
            }

            bottomPositionR = rightHit.point.y;
            if (rightHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedR = true;

            } else {
                isGroundedR = false;
            }
        } else  {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedR = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(bottomCenterCorner, transform.TransformDirection(Vector3.down), out centerHit, probingMaxDistance)) {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * centerHit.distance, Color.red, debugLineLifetime);

            float centerHitAngle = Vector2.Angle(Vector2.up, new Vector2(centerHit.normal.x, centerHit.normal.y));

            if (centerHitAngle > 0) {
                if (centerHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeC = true;
                } else {
                    isOnNonWalkableSlopeC = false;
                }
            } else {
                isOnNonWalkableSlopeC = false;
            }

            bottomPositionC = centerHit.point.y;
            if (centerHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedC = true;

            } else {
                isGroundedC = false;
            }
        } else  {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedC = false;
        }

        isGrounded = isGroundedL || isGroundedR || isGroundedC;
        isOnSlope = isGrounded && isAboveSlope;
        isAboveSlope = (isAboveSlopeL && isAboveSlopeR)
            || (isAboveSlopeL && !isAboveSlopeR && bottomPositionL >= bottomPositionR)
            || (isAboveSlopeR && !isAboveSlopeL && bottomPositionR >= bottomPositionL);
        isOnNonWalkableSlope = isOnNonWalkableSlopeL || isOnNonWalkableSlopeR || isOnNonWalkableSlopeC;

        if (isOnSlope) {
            if (isAboveSlopeL && bottomPositionL - bottomPositionC > groundSlopeOffsetY) {
                nextBottomPosition = bottomPositionL;

            } else if (isAboveSlopeR && bottomPositionR - bottomPositionC > groundSlopeOffsetY) {
                nextBottomPosition = bottomPositionR;
                
            } else {
                nextBottomPosition = bottomPositionC;
            }

        } else {
            nextBottomPosition = Mathf.Max(nextBottomPosition, bottomPositionL, bottomPositionR, bottomPositionC);
        }
        
        moveVector.y = nextBottomPosition - bottomCenterCorner.y + rayBottomOffsetY;

        return moveVector;
    }

    private Vector3 ProbeUpward(Vector3 moveVector)
    {
        float nextTopPosition = topCenterCorner.y + moveVector.y + rayBottomOffsetY;
        float topPositionR = nextTopPosition;
        float topPositionC = nextTopPosition;
        float topPositionL = nextTopPosition;

        if (currentSpeedY > 0) {
            RaycastHit leftHit;
            if (Physics.Raycast(topLeftCorner, transform.TransformDirection(Vector3.up), out leftHit, probingMaxDistance)) {
                Debug.DrawRay(topLeftCorner, transform.TransformDirection(Vector3.up) * leftHit.distance, Color.red, debugLineLifetime);

                topPositionR = leftHit.point.y - groundOffsetY;

                if (topPositionR < nextTopPosition && leftHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
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

                topPositionC = rightHit.point.y - groundOffsetY;

                if (topPositionC < nextTopPosition  && rightHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
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

                topPositionL = centerHit.point.y - groundOffsetY;

                if (topPositionL < nextTopPosition  && centerHit.transform.gameObject.GetComponent<OneSidePlatform>() == null) {
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
                currentSpeedY = 0.0f;
                nextTopPosition = Mathf.Min(nextTopPosition, topPositionL, topPositionR, topPositionC);
                moveVector.y = nextTopPosition - topCenterCorner.y - rayBottomOffsetY;
            }

        }
        return moveVector;
    }

    private Vector3 ProbeSides(Vector3 moveVector)
    {
        float dir = Mathf.Sign(moveVector.x);
        float nextSidePosition = sideCenterCorner.x + moveVector.x + raySideOffsetX * dir;
        float sidePositionT = nextSidePosition;
        float sidePositionB = nextSidePosition;
        float sidePositionC = nextSidePosition;
        float moveSign;

        if (isRunning) {
            RaycastHit topHit;
            if (Physics.Raycast(sideTopCorner, transform.TransformDirection(Vector3.right) * dir, out topHit, probingMaxDistance)) {
                float topHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(topHit.normal.x, topHit.normal.y)) - 90;
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * topHit.distance, Color.red, debugLineLifetime);
                
                sidePositionT = topHit.point.x - groundOffsetX * dir;
                moveSign = Mathf.Sign(nextSidePosition - sidePositionT);
                if (moveSign == dir && topHitAngle > maxSlopeAngle) {
                    isBumpedSideT = true;

                } else {
                    isBumpedSideT = false;
                }
            } else {
                Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideT = false;
            }

            RaycastHit bottomHit;
            if (Physics.Raycast(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir, out bottomHit, probingMaxDistance)) {
                float bottomHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(bottomHit.normal.x, bottomHit.normal.y)) - 90;
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * bottomHit.distance, Color.red, debugLineLifetime);
                
                sidePositionB = bottomHit.point.x - groundOffsetX * dir;
                moveSign = Mathf.Sign(nextSidePosition - sidePositionB);
                if (moveSign == dir && bottomHitAngle > maxSlopeAngle) {
                    isBumpedSideB = true;

                } else {
                    isBumpedSideB = false;
                }
            } else  {
                Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideB = false;
            }

            RaycastHit centerHit;
            if (Physics.Raycast(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir, out centerHit, probingMaxDistance)) {
                float centerHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(centerHit.normal.x, centerHit.normal.y)) - 90;
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * centerHit.distance, Color.red, debugLineLifetime);
                
                sidePositionC = centerHit.point.x - groundOffsetX * dir;
                moveSign = Mathf.Sign(nextSidePosition - sidePositionC);
                if (moveSign == dir && centerHitAngle > maxSlopeAngle) {
                    isBumpedSideC = true;

                } else {
                    isBumpedSideC = false;
                }
            } else  {
                Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
                isBumpedSideC = false;
            }

            isBumpedSide = (isBumpedSideT || isBumpedSideB || isBumpedSideC);

            if (isBumpedSide) {
                currentSpeedX = 0;

                if (!isOnNonWalkableSlope) {
                    if (dir > 0) {
                        nextSidePosition = Mathf.Min(nextSidePosition, sidePositionT, sidePositionB, sidePositionC);

                    } else {
                        nextSidePosition = Mathf.Max(nextSidePosition, sidePositionT, sidePositionB, sidePositionC);
                    }
                } else {
                    nextSidePosition = sideCenterCorner.x + raySideOffsetX * dir;
                }
            }

            moveVector.x = nextSidePosition - sideCenterCorner.x - raySideOffsetX * dir;
            
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
        if (isRunning)
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
