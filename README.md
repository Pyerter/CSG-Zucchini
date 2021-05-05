# CSG-Zucchini
This is a small game that Porter Squires, Michael Zabrowski, Alex Zabrowski, and Nicholas Pautsch are going to be working on over a Summer.


### Work Flow

Our Master Branch will act as the release branch and the structure will follow like so:
```
Master
   |  \__ Developer
   |          |    \__ Feature
   |          |           |   \__ Feature Patch
   |          |           |             |
   |          |           |             |
   |          |           |             |
Master    Developer    Feature    Feature Patch
   |          |           |        /
   |          |        Feature __ /
   |      Developer __ /
Master __ /
```
Whenever we merge a Feature branch into the Developer branch, we will use pull requests so that at least one other collaborator can check for merge conflicts. <br/>
Feature Patches will be small enough there there should not be any merge conflicts.<br/>
When we complete a Feature and merged it into Developer in a working state, we will delete the corresponding Feature branch. If we are to work on that Feature again, we will make another branch for that Feature with the same name.
