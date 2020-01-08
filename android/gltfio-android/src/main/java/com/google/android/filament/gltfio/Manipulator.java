/*
 * Copyright (C) 2020 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.google.android.filament.gltfio;

import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.annotation.Size;

/**
 * Helper that enables camera interaction similar to sketchfab or Google Maps.
 *
 * Clients notify the camera manipulator of various mouse or touch events, then periodically call
 * its getLookAt() method so that they can adjust their camera(s). Two modes are supported: ORBIT
 * and MAP. To construct a manipulator instance, the desired mode is passed into the create method.
 */
public class Manipulator {
    private long mNativeObject;
    private Mode mMode;

    public enum Mode { ORBIT, MAP };

    public enum Fov { VERTICAL, HORIZONTAL };

    /**
     * Construction properties that are never changed by the manipulator.
     *
     * Clients are required to specify a viewport but all other properties have reasonable defaults.
     * Any properties that are mutable have corresponding set methods in Manipulator.
     */
    public class Config {
        public int viewport[] = new int[] {0, 0};              //! Width and height of the viewing area
        public float targetPosition[] = new float[] {0, 0, 0}; //! World-space position of interest
        public float upVector[] = new float[] {0, 1, 0};       //! Orientation for the home position
        public float zoomSpeed = 0.01f;                        //! Multiplied with scroll delta

        // Orbit mode properties
        public float orbitHomePosition[] = new float[] {0, 0, 1}; //! Initial eye position in world space
        public float orbitSpeed[] = new float[] {0.01f, 0.01f};   //! Multiplied with viewport delta

        // Map mode properties
        public Fov fovDirection = Fov.VERTICAL;           //! The FOV axis that's held constant when the viewport changes
        public float fovDegrees = 33;                     //! The full FOV (not the half-angle)
        public float farPlane = 5000;                     //! The distance to the far plane
        public float mapExtent[] = new float[]{512, 512}; //! The ground plane size used to compute the home position
        public float mapMinDistance = 0.0f;               //! Constrains the zoom-in level

        // Raycast properties
        public float groundPlane[] = new float[] {0, 0, 1, 0};
    };

    /**
     * Creates a new camera manipulator, either ORBIT or MAP.
     *
     * Call destroy() when done.
     */
    public static Manipulator create(Mode mode, Config props) {
        return null;
    }

    @Override
    public void finalize() {
        try {
            super.finalize();
        } catch (Throwable t) { // Ignore
        } finally {
            // nDestroyManipulator(mNativeObject);
        }
    }

    /**
     * Gets the immutable mode of the manipulator.
     */
    public Mode getMode() { return mMode; }

    /**
     * Sets the viewport dimensions. The manipulator uses this processing grab events and raycasts.
     */
    void setViewport(int width, int height) {}

    /**
     * Gets the current orthonormal basis. This is usually called once per frame.
     */
    public void getLookAt(
            @NonNull @Size(min = 3) float[] eyePosition,
            @NonNull @Size(min = 3) float[] targetPosition,
            @NonNull @Size(min = 3) float[] upward) {}

    /**
     * Given a viewport coordinate, picks a point in the ground plane.
     */
    @Nullable @Size(min = 3)
    public float[] raycast(int x, int y) {
        return null;
    }

    /**
     * Starts a grabbing session (i.e. the user is dragging around in the viewport).
     *
     * This starts a panning session in MAP mode, and start either rotating or strafing in ORBIT.
     *
     * @param x X-coordinate for point of interest in viewport space
     * @param y Y-coordinate for point of interest in viewport space
     * @param strafe ORBIT mode only; if true, starts a translation rather than a rotation.
     */
    public void grabBegin(int x, int y, Boolean strafe) {}

    /**
     * Updates a grabbing session.
     *
     * This must be called at least once between grabBegin / grabEnd to dirty the camera.
     */
    public void grabUpdate(int x, int y) {}

    /**
     * Ends a grabbing session.
     */
    public void grabEnd(int x, int y) {}

    /**
     * Dollys the camera along the viewing direction.
     *
     * @param x X-coordinate for point of interest in viewport space
     * @param y Y-coordinate for point of interest in viewport space
     * @param scrolldelta Positive means "zoom in", negative means "zoom out"
     */
    public void zoom(int x, int y, float scrolldelta) {}

    /**
     * Gets a handle that can be used to reset the manipulator back to its current position.
     *
     * \see jumpToBookmark
     */
    public Bookmark getCurrentBookmark() {
        return null;
    }

    /**
     * Gets a handle that can be used to reset the manipulator back to its home position.
     *
     * see jumpToBookmark
     */
    public Bookmark getHomeBookmark() {
        return null;
    }

    /**
     * Sets the manipulator position and orientation back to a stashed state.
     *
     * \see getCurrentBookmark, getHomeBookmark
     */
    public void jumpToBookmark(Bookmark bookmark) {
    }
}
