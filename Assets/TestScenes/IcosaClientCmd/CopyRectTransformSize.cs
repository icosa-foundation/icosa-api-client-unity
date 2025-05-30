// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IcosaToolkitDev {

public class CopyRectTransformSize : MonoBehaviour {
  private RectTransform myRectTransform;
  public RectTransform copyFrom;
  public Vector2 delta = Vector2.zero;

  private void Start() {
    myRectTransform = GetComponent<RectTransform>();
  }

  void Update () {
		myRectTransform.sizeDelta = copyFrom.sizeDelta + delta;
	}
}

}