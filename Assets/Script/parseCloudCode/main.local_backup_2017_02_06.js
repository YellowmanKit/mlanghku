const NO_ERROR = 0;
const ERROR_ADD_PROJECT_ALLOW_SUBMISSION_AFTER_DUE_DATE = 1;
const ERROR_ADD_PROJECT_DUE_DATE_BEFORE_TODAY = 2;
const ERROR_ADD_PROJECT_NO_IMAGE = 3;
const ERROR_ADD_PROJECT_DUPLICATE_NAME = 7;

const ERROR_STUDENT_NO_COURSE = 4;

const ERROR_PROFILE_INCORRECT_ORIGIN_PASSWORD = 5;


const ERROR_PERMISSION = 6;

const ERROR_INVALID_TIMESTAMP = 99;


// Use Parse.Cloud.define to define as many cloud functions as you want.
// For example:
/*
Parse.Cloud.define("saveCard", function(request, response) {
    var cardName = request.params.cardName;
    var cardImage = request.params.cardImage;
    var cardSound = request.params.cardSound;

    var CardClass = Parse.Object.extend("Card");
    var card = new CardClass();
    card.set("name", cardName);
    card.set("image", cardImage);
    card.set("sound", cardSound);
    card.save(null,{
        success: function(object) {
            response.success();
        },
        error: function(error) {
            response.error(error);
        }
    });
});
*/


Parse.Cloud.beforeSave(Parse.User, function(request, response) {
    if (!request.object.get("identity")) {
        request.object.set("identity", 0);
    }
    if (!request.object.get("banned")) {
        request.object.set("banned", 0);
    }
	if (request.object.get("identity") == 2) {
		if (!request.object.get("comments")) {
			request.object.set("comments", ["Incorrect English Name", "Incorrect Chinese Name", "Excellent"]);
		}
    }

	if (request.object.isNew()) {
		request.object.set("numOfTeacherAttention", 0);
		request.object.set("numOfFeaturedCards", 0);
		request.object.set("numOfSubmittedCards", 0);
		response.success();
	} else {
		var query = new Parse.Query("StudentProject");
		query.equalTo("student", request.object);
		query.find({
			success: function(studentProjects) {

				var numOfFeaturedCards = 0;
				var numOfSubmittedCards = 0;
				var numOfTeacherAttention = 0;
				for (var i = 0; i < studentProjects.length; i++) {
					var studentProject = studentProjects[i];

					var featured = studentProject.get("numOfFeaturedCards");
					var submitted = studentProject.get("numOfSubmittedCard");
					var teacherAttention = studentProject.get("numOfTeacherAttention");

					numOfFeaturedCards += featured;
					numOfSubmittedCards += submitted;
					if (teacherAttention > 0)
						numOfTeacherAttention++;
				}

				request.object.set("numOfTeacherAttention", numOfTeacherAttention);
				request.object.set("numOfFeaturedCards", numOfFeaturedCards);
				request.object.set("numOfSubmittedCards", numOfSubmittedCards);
				response.success();
			},
			error: function(error) {
				response.success();
			}
		});
	}

    //response.success();
});


Parse.Cloud.afterDelete(Parse.User, function(request) {
	var identity = request.object.get("identity");
	if (identity == 1) {
		// remove student project
		/*
		var query = new Parse.Query("StudentProject");
		query.equalTo("student", request.object);
		query.find().then(function(studentProjects) {
			return Parse.Object.destroyAll(studentProjects);
		}).then(function(success) {
			// The related comments were deleted
		}, function(error) {
			console.error("Error deleting related studentProjects " + error.code + ": " + error.message);
		});
		*/

		var query = new Parse.Query("Card");
		query.equalTo("author", request.object);
		query.find().then(function(cards) {
			var pendingToSave = [];
			for (var i = 0; i < cards.length; i++) { 	// enable to get card by carId later
				var card = cards[i];
				card.increment("lastUpdatedAt", 1);
				card.set("isDeleted", 1);
				pendingToSave.push(card);
			}
			Parse.Object.saveAll(pendingToSave, {
				success: function(saved) {

				}
			});
		});
	} else if (identity == 2) {
		// unset the courseTeacher of Course
		var query = new Parse.Query("Course");
		query.equalTo("courseTeacher", request.object);
		query.find({
			success: function(courseResults) {
				if (courseResults.length > 0) {
					var pendingToSave = [];
					for (var i = 0; i < courseResults.length; i++) { 	// enable to get card by carId later
						var course = courseResults[i];
						course.unset("courseTeacher");
						pendingToSave.push(course);
					}
					Parse.Object.saveAll(pendingToSave, {
						success: function(savedLangs) {

						}
					});
				}
			},
			error: function(error) {
				console.error(error.code + ": " + error.message);
			}
		});
	}
});

//Parse.Cloud.afterDelete("StudentProject", function(request) {
	/*
	var query = new Parse.Query("Card");
	query.equalTo("studentProject", request.object);
	query.find().then(function(cards) {
		return Parse.Object.destroyAll(cards);
	}).then(function(success) {
		// The related cards were deleted
	}, function(error) {
		console.error("Error deleting related cards " + error.code + ": " + error.message);
	});
	**/
	/*
	var query = new Parse.Query("Card");
	query.equalTo("studentProject", request.object);
	query.find().then(function(cards) {
		var pendingToSaveCards = [];
		for (var i = 0; i < cards.length; i++) { 	// enable to get card by carId later
			var card = cards[i];
			card.increment("lastUpdatedAt", 1);
			card.set("isDeleted", 1);
			pendingToSaveCards.push(card);
		}
	});
	*/
//});

Parse.Cloud.afterDelete("Card", function(request) {
	var cardId = request.object.get("cardId");
	var query = new Parse.Query("CardLang");
	query.equalTo("cardId", cardId);
	query.find().then(function(cardLangs) {
		return Parse.Object.destroyAll(cardLangs);
	}).then(function(success) {
		// The related cards were deleted
		if (!request.object.get("studentProject")) {

		} else {
			var queryProject = new Parse.Query("StudentProject");
			queryProject.get(request.object.get("studentProject").id, {
				success: function(studentProject) {
					studentProject.save();
				},
				error: function(error) {
					console.error(error);
				}
			});
		}
	}, function(error) {
		console.error("Error deleting related cardLangs " + error.code + ": " + error.message);
	});
});

Parse.Cloud.afterDelete("Course", function(request) {
	var query = new Parse.Query("Project");
	query.equalTo("course", request.object);
	query.find().then(function(projects) {
		return Parse.Object.destroyAll(projects);
	}).then(function(success) {
		// The related projects
		var query = new Parse.Query(Parse.User);
		query.equalTo("studentOfCourse", request.object);
		query.find().then(function(students) {
			Parse.Cloud.useMasterKey();
			return Parse.Object.destroyAll(students);
		}).then(function(success) {
			// The related cards were deleted
		}, function(error) {
			console.error("Error deleting related cards " + error.code + ": " + error.message);
		});
	}, function(error) {
		console.error("Error deleting related cards " + error.code + ": " + error.message);
	});
});

Parse.Cloud.afterDelete("Project", function(request) {
	var query = new Parse.Query("StudentProject");
	query.equalTo("project", request.object);
	query.find().then(function(studentProjects) {
		return Parse.Object.destroyAll(studentProjects);
	}).then(function(success) {
		// The related cards were deleted
	}, function(error) {
		console.error("Error deleting related cards " + error.code + ": " + error.message);
	});
});

Parse.Cloud.afterDelete("School", function(request) {
	var query = new Parse.Query("Course");
	query.equalTo("school", request.object);
	query.find().then(function(courses) {
		return Parse.Object.destroyAll(courses);
	}).then(function(success) {
		// The related cards were deleted
		var query = new Parse.Query(Parse.User);
		query.equalTo("school", request.object);
		query.find().then(function(staffs) {
			Parse.Cloud.useMasterKey();
			return Parse.Object.destroyAll(staffs);
		}).then(function(success) {
			// The related cards were deleted
			var query = new Parse.Query("SchoolYear");
			query.equalTo("school", request.object);
			query.find().then(function(years) {
				return Parse.Object.destroyAll(years);
			}).then(function(success) {
				// The related cards were deleted
			}, function(error) {
				console.error("Error deleting related cards " + error.code + ": " + error.message);
			});
		}, function(error) {
			console.error("Error deleting related users " + error.code + ": " + error.message);
		});
	}, function(error) {
		console.error("Error deleting related courses " + error.code + ": " + error.message);
	});
});


Parse.Cloud.beforeSave("Card", function(request, response) {

    if (!request.object.get("isOld")) {
        request.object.set("isOld", 0);
    }
    if (!request.object.get("isDeleted")) {
        request.object.set("isDeleted", 0);
    }
    if (!request.object.get("lastUpdatedAt")) {
        request.object.set("lastUpdatedAt", 0);
    }
    if (!request.object.get("status")) {
        request.object.set("status", 0);
    }

	if (!request.object.get("studentProject")) {
		if (request.object.get("status") != 3) {
			// not belong to student project and not featured card, not visible to anyone
			request.object.set("isDeleted", 1);
		}
	}


	// shoud student pay attention?
	var status = request.object.get("status");
	var commentArray = request.object.get("comments");

	if (status == 0) {
		// not graded
		request.object.set("studentAttention", 0);
        request.object.set("teacherAttention", 1);
	} else if (status == 1) {
		// passed
		request.object.set("studentAttention", 0);
        request.object.set("teacherAttention", 0);
	} else if (status == 2) {
		// failed
		request.object.set("studentAttention", 1);
        request.object.set("teacherAttention", 0);
	} else if (status == 3) {
		// featured
		request.object.set("studentAttention", 0);
        request.object.set("teacherAttention", 0);
	}

    if (!request.object.get("teacherAttention")) {
        request.object.set("teacherAttention", 0);
    }

    response.success();
});


Parse.Cloud.beforeSave("CardLang", function(request, response) {

    if (!request.object.get("isOld")) {
        request.object.set("isOld", 0);
    }
    if (!request.object.get("isDeleted")) {
        request.object.set("isDeleted", 0);
    }

    response.success();
});

Parse.Cloud.afterSave("Card", function(request, response) {
	// save studentProject
	if (!request.object.get("studentProject")) {

	} else {
		var queryProject = new Parse.Query("StudentProject");
		queryProject.get(request.object.get("studentProject").id, {
			success: function(studentProject) {
				studentProject.save();
			}
		});
	}
});

Parse.Cloud.beforeSave("StudentProject", function(request, response) {
	if (request.object.isNew()) {
		request.object.set("numOfSubmittedCard", 0);
		request.object.set("numOfFeaturedCards", 0);
		request.object.set("numOfStudentAttention", 0);
		request.object.set("numOfTeacherAttention", 0);
		response.success();
	} else {
		//response.success();
		//var query = request.object.relation("cards").query();
		var query = new Parse.Query("Card");
		query.equalTo("isOld", 0);
		query.equalTo("isDeleted", 0);
		query.equalTo("studentProject", request.object);
		query.find({
			success: function(cards) {
				var numOfCard = cards.length;
				var numOfFeaturedCards = 0;
				var numOfStudentAttention = 0;
				var numOfTeacherAttention = 0;
				for (var i = 0; i < cards.length; i++) {
					var card = cards[i];

					var studentAttention = card.get("studentAttention");
					var teacherAttention = card.get("teacherAttention");
					var status = card.get("status");

					if (studentAttention == 1)
						numOfStudentAttention++;
					if (teacherAttention == 1) {
						console.log("-------------" + card.id);
						numOfTeacherAttention++;
					}
					if (status == 3)
						numOfFeaturedCards++;
				}

				// not yet accurate, save the "StduentProejct" again
				request.object.set("numOfSubmittedCard", numOfCard);
				request.object.set("numOfFeaturedCards", numOfFeaturedCards);
				request.object.set("numOfStudentAttention", numOfStudentAttention);
				request.object.set("numOfTeacherAttention", numOfTeacherAttention);
				response.success();
			}
		});
	}
});

Parse.Cloud.afterSave("StudentProject", function(request, response) {

	var queryProject = new Parse.Query("Project");
	queryProject.get(request.object.get("project").id, {
		success: function(project) {
			project.save();

			var queryUser = new Parse.Query(Parse.User);
			queryUser.get(request.object.get("student").id, {
				success: function(user) {
					Parse.Cloud.useMasterKey();
					user.save();
				}
			});
		}
	});
});

Parse.Cloud.beforeSave("Project", function(request, response) {

	if (request.object.isNew()) {
		request.object.set("numOfTeacherAttention", 0);
		request.object.set("numOfStudentDone", 0);
		request.object.set("isDeleted", 0);
		response.success();
	} else {
		if (!request.object.get("isDeleted")) {
			request.object.set("isDeleted", 0);
		}
		var query = new Parse.Query("StudentProject");
		query.equalTo("project", request.object);
		query.greaterThan("numOfTeacherAttention", 0);
		query.count({
			success: function(numOfTeacherAttention) {
				request.object.set("numOfTeacherAttention", numOfTeacherAttention);


				var queryDone = new Parse.Query("StudentProject");
				queryDone.equalTo("project", request.object);
				queryDone.greaterThan("numOfSubmittedCard", 0);
				queryDone.count({
					success: function(numOfStudentDone) {
						request.object.set("numOfStudentDone", numOfStudentDone);
						response.success();
					},
					error: function(error) {
						response.error();
					}
				});
/*
				var queryFeaturedCard = new Parse.Query("Card");
				queryFeaturedCard.equalTo("project", request.object);
				queryFeaturedCard.equalTo("status", 3);
				queryFeaturedCard.count({
					success: function(numOfFeatured) {
						var originNumOfFeatured = request.object.get("numOfFeatured");
						if (numOfFeatured != originNumOfFeatured) {
							var currentTime =  Math.floor(new Date().getTime() / 1000);
							request.object.set("numOfFeatured", numOfFeatured);
							request.object.set("featureUpdatedAt", currentTime);
						}
						response.success();
					},
					error: function(error) {
						response.error();
					}
				});
				*/
			},
			error: function(error) {
				response.error();
			}
		});
	}
});

Parse.Cloud.afterSave("Project", function(request, response) {

	var queryCourse = new Parse.Query("Course");
	queryCourse.get(request.object.get("course").id, {
		success: function(course) {
			course.save();
		}
	});
});

Parse.Cloud.beforeSave("Course", function(request, response) {

	if (request.object.isNew()) {
		request.object.set("numOfTeacherAttention", 0);
		response.success();
	} else {
		var query = new Parse.Query("Project");
		query.equalTo("course", request.object);
		query.equalTo("isDeleted", 0);
		query.greaterThan("numOfTeacherAttention", 0);
		query.count({
			success: function(number) {
				request.object.set("numOfTeacherAttention", number);
				response.success();
			},
			error: function(error) {
				response.error();
			}
		});
	}
});

function PreprocessUser (user) {
  if (user == null) // appear on server's copy
		return "{}";   // appear on server's copy
    var json = user.toJSON();

    return JSON.stringify(json);
}

function PreprocessStudentProjectCards (cards) {
    //var json = cards.toJSON();

	var output = {};

	for (var i = 0; i < cards.length; i++) {
		var cardObj = cards[i];
		var card = cardObj.toJSON();
		var cardId = card["cardId"];
		var lastUpdatedAt = card["lastUpdatedAt"];

		var existedLastUpdatedAt = 0;
		if (!output[cardId] || !output[cardId]["lastUpdatedAt"])
			existedLastUpdatedAt = 0;
		else {
			existedLastUpdatedAt = output[cardId]["lastUpdatedAt"];
		}

		if (lastUpdatedAt > existedLastUpdatedAt && card["isOld"] == 0) {
			delete card["createdAt"];
			delete card["updatedAt"];
			output[cardId] = card;
		}
	}

    return JSON.stringify(output);
}


function PreprocessFeaturedCards (projectIds, featuredCards) {
	var jsonFeaturedCards = {};
	for (var i = 0; i < projectIds.length; i++) {
		var projectId = projectIds[i];
		jsonFeaturedCards[projectId] = {};
	}
	for (var i = 0; i < featuredCards.length; i++) {

		var featuredCardObj = featuredCards[i];
		var objectId = featuredCardObj.id;
		var cardId = featuredCardObj.get("cardId");

		var userObj = featuredCardObj.get("author");
		if (userObj != null) {
			var userNode = userObj.toJSON();
			var authorName = userNode["username"];
			var authorRealName = userNode["realName"];
			var authorId = userNode["objectId"];
			delete userNode["identity"];
			delete userNode["numOfTeacherAttention"];
			delete userNode["studentOfCourse"];
			delete userNode["createdAt"];
			delete userNode["updatedAt"];
		} else {
			var authorName = "";
			var authorRealName = "";
			var authorId = "";
		}

		var projectObj = featuredCardObj.get("project");

		var featuredCardNode = (featuredCardObj.toJSON());
		delete featuredCardNode["createdAt"];
		delete featuredCardNode["updatedAt"];
		delete featuredCardNode["isOld"];
		delete featuredCardNode["isDeleted"];
		delete featuredCardNode["teacherAttention"];
		delete featuredCardNode["studentAttention"];
		delete featuredCardNode["studentProject"];
		delete featuredCardNode["author"];

		var projectId = featuredCardNode["project"]["objectId"];

		//console.log("**" + projectId);

		jsonFeaturedCards[projectId][cardId] = featuredCardNode;
		jsonFeaturedCards[projectId][cardId]["authorId"] = authorId;
		jsonFeaturedCards[projectId][cardId]["authorName"] = authorName;
		jsonFeaturedCards[projectId][cardId]["authorRealName"] = authorRealName;
	}
	return jsonFeaturedCards;
}

function ValidTime (timestamp) {
    var currentTime =  Math.floor(new Date().getTime() / 1000);

	return Math.abs(currentTime - timestamp) <= 3600;
}

function CoursesHandle(courseResults) {

    var coursesJson = {};
    for (var i = 0; i < courseResults.length; i++) {

        var courseObj = courseResults[i];
        var objectId = courseObj.id;

        var courseJSON = (courseObj.toJSON());
        delete courseJSON["createdAt"];

        coursesJson[objectId] = courseJSON;
    }

    return JSON.stringify(coursesJson);
}

function SchoolsHandle(rs) {

    var json = {};
    for (var i = 0; i < rs.length; i++) {

        var obj = rs[i];
        var objectId = obj.id;

        var j = (obj.toJSON());
        delete j["createdAt"];

        json[objectId] = j;
    }

    return JSON.stringify(json);
}

function ProjectsHandle(projectResults) {

    var projectsJSON = {};
    for (var i = 0; i < projectResults.length; i++) {

        var projectObj = projectResults[i];
        var objectId = projectObj.id;

        var projectJSON = (projectObj.toJSON());
        delete projectJSON["createdAt"];

        projectsJSON[objectId] = projectJSON;
    }

    return JSON.stringify(projectsJSON);
}

Parse.Cloud.define("RenewUser", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var identity = user.get("identity");

	if (identity == 1) {
		// student
		var belongToCourse = user.get("studentOfCourse");
		if (belongToCourse == null) {
			response.error(ERROR_STUDENT_NO_COURSE);
			return;
		}

		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("objectId", belongToCourse.id);
		queryCourse.descending("createdAt");
		queryCourse.first({
			success: function(course) {
				// get projects list
				var queryProject = new Parse.Query("Project");
				queryProject.equalTo("course", belongToCourse);
				queryProject.equalTo("isDeleted", 0);
				queryProject.descending("dueDate");
				queryProject.find({
					success: function(projectResults) {

						// get submitted project
						var querySubmittedProject = new Parse.Query("StudentProject");
						querySubmittedProject.equalTo("student", user);
						querySubmittedProject.find({
							success: function(projectSubmittedResults) {

								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										",\"my-projects\":" + JSON.stringify(projectResults) +
										",\"my-projects-submitted\":" + JSON.stringify(projectSubmittedResults) +
										",\"my-course\":" + JSON.stringify(course) +
										"}");

							},
							error: function() {
								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										",\"my-projects\":" + JSON.stringify(projectResults) +
										",\"my-course\":" + JSON.stringify(course) +
										"}");
							}
						});


					},
					error: function() {
						response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"user\":" + PreprocessUser(user) +
								",\"my-course\":" + JSON.stringify(course) +
								"}");
					}
				});

			},
			error: function() {
				response.error("Unknown error");
			}
		});

	} else if (identity == 2) {
		// teacher
		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("courseTeacher", user);
		queryCourse.descending("createdAt");
		queryCourse.find({
			success: function(courseResults) {
				if (courseResults.length > 0) {
					// also find the project

					var queryProject = new Parse.Query("Project");
					//queryProject.equalTo("course", belongToCourse);
					queryProject.containedIn("course", courseResults);
					queryProject.equalTo("isDeleted", 0);
					queryProject.descending("dueDate");
					queryProject.find({
						success: function(projectResults) {
							response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"courses\":" + CoursesHandle(courseResults) +
								",\"my-projects\":" + JSON.stringify(projectResults) +
								",\"user\":" + PreprocessUser(user) +
								"}");


						},
						error: function() {
							response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"courses\":" + CoursesHandle(courseResults) +
								",\"user\":" + PreprocessUser(user) +
								"}");
						}
					});
				} else {
				   response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"user\":" + PreprocessUser(user) +
							"}");

				}
			},
			error: function() {
				response.error("Unknown error");
			}
		});
	} else if (identity == 3) {
		// school admin
		var school = user.get("school");
		var queryTeacher = new Parse.Query(Parse.User);
		queryTeacher.equalTo("school", school);
		queryTeacher.equalTo("identity", 2);

		var querySchoolAdmin = new Parse.Query(Parse.User);
		querySchoolAdmin.equalTo("school", school);
		querySchoolAdmin.equalTo("identity", 3);

		var queryCombine = Parse.Query.or(queryTeacher, querySchoolAdmin);

		queryCombine.find({
			success: function(teacherResults) {
				var queryCourse = new Parse.Query("Course");
				queryCourse.equalTo("school", school);
				queryCourse.find({
					success: function(courseResults) {
						response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"school\":" + JSON.stringify(school.toJSON()) +
								",\"courses\":" + CoursesHandle(courseResults) +
								",\"teachers\":" + JSON.stringify(teacherResults) +
								",\"user\":" + PreprocessUser(user) +
								"}");
					}
				});
			}
		});
	} else if (identity == 4) {
		// system admin
		var querySchool = new Parse.Query("School");
		querySchool.ascending("abbreviation");
		querySchool.find({
			success: function(schoolRs) {
				if (schoolRs.length > 0) {
					// also find the project

					response.success("{\"success\":" + NO_ERROR +
						",\"currentTime\":\"" + currentTime + "\"" +
						",\"schools\":" + CoursesHandle(schoolRs) +
						",\"user\":" + PreprocessUser(user) +
						"}");
				} else {
				   response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"user\":" + PreprocessUser(user) +
							"}");

				}
			},
			error: function() {
				response.error("Unknown error");
			}
		});
	} else {
		response.error("Unknown identity");
	}
});

Parse.Cloud.define("GetCourseData", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var courseId = request.params.courseId;

	var identity = user.get("identity");

	var Course = Parse.Object.extend("Course");
	var pointerCourse = new Course();
	pointerCourse.id = courseId;

	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("studentOfCourse", pointerCourse);
	queryUser.find({
		success: function(studentResults) {
			var queryProject = new Parse.Query("Project");
			queryProject.equalTo("course", pointerCourse);
			queryProject.equalTo("isDeleted", 0);
			queryProject.find({
				success: function(projectResults) {
					response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"students\":" + JSON.stringify(studentResults) +
							",\"projects\":" + JSON.stringify(projectResults) +
							"}");
				},
				error: function() {
					response.error("queryProject error");
				}
			});
		},
		error: function() {
			response.error("queryUser error");
		}
	});
});

Parse.Cloud.define("Teacher_AddProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	// receive parameter
    var objectId = request.params.objectId;	// if exist, it is edit

    var projectTitle = request.params.projectTitle;
    var projectDesc = request.params.projectDesc;

    var isDeleted = parseInt(request.params.isDeleted);

    var courseId = request.params.courseId;
	var Course = Parse.Object.extend("Course");
	var pointerCourse = new Course();
	pointerCourse.id = courseId;

    var createdBy = user;
    var projectIcon = request.params.projectIcon;

    var dueDate = request.params.dueDate;

	var identity = user.get("identity");
	if (identity <= 1) {
        response.error(ERROR_PERMISSION);
		return;
	}

	var currentDate = new Date();
	if (dueDate <= currentDate) {
        response.error(ERROR_ADD_PROJECT_DUE_DATE_BEFORE_TODAY);
		return;
	}

    var ProjectClass = Parse.Object.extend("Project");
    var project = new ProjectClass();
	if (!objectId) {
		// new
		if (projectIcon == null) {
			response.error(ERROR_ADD_PROJECT_NO_IMAGE);
			return;
		} else {
			project.set("projectIcon", projectIcon);
		}
	} else {
		// edit
		project.id = objectId;
		if (projectIcon != null) {
			project.set("projectIcon", projectIcon);
		}
	}
    project.set("projectTitle", projectTitle);
    project.set("projectDesc", projectDesc);
    project.set("course", pointerCourse);
    project.set("createdBy", createdBy);
    project.set("isDeleted", isDeleted);

    project.set("dueDate", dueDate);

    project.set("type", 1);
    project.set("courseIconUpdatedAt", currentTime);

	// project with same name?
	var queryExistProject = new Parse.Query("Project");
	queryExistProject.equalTo("projectTitle", projectTitle);
	queryExistProject.equalTo("course", pointerCourse);
	queryExistProject.equalTo("isDeleted", 0);
	queryExistProject.first({
		success: function(existedProject) {
			var duplicateName = false;
			// project with same name?
			if (existedProject == null) {
				// must be no problem
				// save directly
			} else {
				// check if it is the same object id,
				// if yes, no problem, editing this project
				// if no, there is problem. trying to modify an new project which name is duplicated with another project -> reponse error
				if (existedProject.id != objectId) {
					duplicateName = true;
				}
			}

			if (duplicateName) {
				response.error(ERROR_ADD_PROJECT_DUPLICATE_NAME);
			} else {
				// save
				project.save(null,{
					success: function(object) {

						var queryUser = new Parse.Query(Parse.User);
						queryUser.equalTo("studentOfCourse", pointerCourse);
						queryUser.find({
							success: function(studentResults) {
								var queryProject = new Parse.Query("Project");
								queryProject.equalTo("isDeleted", 0);
								queryProject.equalTo("course", pointerCourse);
								queryProject.find({
									success: function(projectResults) {
										response.success("{\"success\":" + NO_ERROR +
												",\"currentTime\":\"" + currentTime + "\"" +
												",\"students\":" + JSON.stringify(studentResults) +
												",\"projects\":" + JSON.stringify(projectResults) +
												"}");
									},
									error: function() {
										response.error("queryProject error");
									}
								});
							},
							error: function() {
								response.error("queryUser error");
							}
						});

					},
					error: function(error) {
						response.error(error);
					}
				});
			}
		},
		error: function() {
			response.error("queryProject error");
		}
	});
});


Parse.Cloud.define("Teacher_SaveCustomizeComments", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	// receive parameter
    var comments = request.params.comments;
	user.set("comments", comments);

	user.save(null,{
		success: function(object) {
			response.success("{\"success\":" + NO_ERROR +
					",\"currentTime\":\"" + currentTime + "\"" +
					",\"user\":" + PreprocessUser(object) +
					"}");
		},
		error: function(error) {
			//console.log("4");
			response.error(error);
		}
	});
});

Parse.Cloud.define("UpdateMyProfile", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var username = user.get("username");
    var originPassword = request.params.originPassword;
    var newPassword = request.params.newPassword;
    var email = request.params.email;

	// change email
	if (email == "") {
	} else {
		user.set("email", email);
	}
	if (newPassword) {

		var currentSession = user.getSessionToken();

		// check origin password
		Parse.User.logIn(username,originPassword, {
			success: function(user) {
				user.setPassword(newPassword);
				//user.setEmail(email);
				Parse.Cloud.useMasterKey();
				user.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"user\":" + PreprocessUser(object) +
								"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			},
			error: function(error) {
				Parse.User.become (currentSession);	// recover original user
				response.error(ERROR_PROFILE_INCORRECT_ORIGIN_PASSWORD);
			}
		});
		/*
		var queryUser = new Parse.Query(Parse.User);
		queryUser.equalTo("objectId", user.id);
		queryUser.first({
			success: function(targetUser) {
				targetUser.setPassword(newPassword);
				if (email == "") {
				} else {
					targetUser.set("email", email);
				}
				Parse.Cloud.useMasterKey();

				targetUser.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			},
			error: function(error) {
				response.error(error);
			}
		});
		*/
	} else {
		user.save(null,{
			success: function(object) {
				//console.log("5");
				response.success("{\"success\":" + NO_ERROR +
						",\"currentTime\":\"" + currentTime + "\"" +
						",\"user\":" + PreprocessUser(user) +
						"}");
			},
			error: function(error) {
				response.error(error);
			}
		});
	}
});

function GetAllCardId (array, cards) {
	for (var i = 0; i < cards.length; i++) {
		var card = cards[i];
		array.push(card.get("cardId"));
	}
}

function GetQuery_FeaturedCards () {
	var query = new Parse.Query("Card");
	var keys = ["objectId", "lastUpdatedAt", "imageLastUpdatedAt", "cardId", "image", "isOld", "isDeleted", "status", "comments", "author", "project"];
	query.select(keys);
	query.equalTo("isOld", 0);
	query.equalTo("status", 3);

	return query;
}

function GetQuery_StudentProjectCards (studentProject) {
	var relation = studentProject.relation("cards");
	var keys = ["objectId", "lastUpdatedAt", "imageLastUpdatedAt", "cardId", "image", "isOld", "isDeleted", "status", "comments"];
	var query = relation.query();
	query.select(keys);
	query.equalTo("isOld", 0);

	return query;
}

function GetQuery_Langs () {
	var query = new Parse.Query("CardLang");
	var keys = ["objectId", "cardId", "langKey", "name", "sound", "isOld", "isDeleted", "soundLastUpdatedAt"];
	query.select(keys);
	query.equalTo("isOld", 0);
	query.equalTo("isDeleted", 0);
	return query;
}

Parse.Cloud.define("Student_Get_SpecificStudentProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var projectId = request.params.projectId;
	var Project = Parse.Object.extend("Project");
	var pointerProject = new Project();
	pointerProject.id = projectId;

	var queryFeaturedCard = GetQuery_FeaturedCards();
	queryFeaturedCard.equalTo("project", pointerProject);
	queryFeaturedCard.include("author");
	queryFeaturedCard.find({
		success: function(featuredCards) {
			// handle featured cards' student data
			var projectIds = [projectId];
			var jsonFeaturedCards = PreprocessFeaturedCards(projectIds, featuredCards);

			var querySubmittedProject = new Parse.Query("StudentProject");
			querySubmittedProject.equalTo("student", user);
			querySubmittedProject.equalTo("project", pointerProject);
			querySubmittedProject.find({
				success: function(projectSubmittedResults) {
						if (projectSubmittedResults.length > 0) {
							var submittedProject = projectSubmittedResults[0];
							var query = GetQuery_StudentProjectCards(submittedProject);
							query.find({
								success: function(cards) {
									var cardIds = new Array();
									GetAllCardId(cardIds, cards);
									GetAllCardId(cardIds, featuredCards);

									var langsQuery = GetQuery_Langs();
									langsQuery.containedIn("cardId", cardIds);	// fetch these cards
									langsQuery.find({
										success: function(langsRs) {
											response.success("{\"success\":" + NO_ERROR +
													",\"currentTime\":\"" + currentTime + "\"" +
													",\"user\":" + PreprocessUser(user) +
													",\"studentProject\":" + JSON.stringify(submittedProject) +
													",\"cards\":" + PreprocessStudentProjectCards(cards) +
													",\"featuredCards\":" + JSON.stringify(jsonFeaturedCards) +
													",\"langs\":" + JSON.stringify(langsRs) +
													"}");
										}

									});
								}
							});
						} else {
							// create new
							var StudentProjectSubmitted = Parse.Object.extend("StudentProject");
							var studentProject = new StudentProjectSubmitted();
							studentProject.set("student", user);
							studentProject.set("project", pointerProject);
							studentProject.save(null,{
								success: function(object) {

									var cardIds = new Array();
									GetAllCardId(cardIds, featuredCards);

									var langsQuery = GetQuery_Langs();
									langsQuery.containedIn("cardId", cardIds);	// fetch these cards
									langsQuery.find({
										success: function(langsRs) {
											response.success("{\"success\":" + NO_ERROR +
													",\"currentTime\":\"" + currentTime + "\"" +
													",\"user\":" + PreprocessUser(user) +
													",\"studentProject\":" + JSON.stringify(object) +
													",\"featuredCards\":" + JSON.stringify(jsonFeaturedCards) +
													",\"langs\":" + JSON.stringify(langsRs) +
													"}");
										}
									});
								},
								error: function(error) {
									response.error(error);
								}
							});
						}


				},
				error: function(error) {
					response.error(error);
				}
			});

		},
		error: function(error) {
			response.error(error);
		}
	});

});

function jsonLength (node) {
	return Object.keys(node).length;
}

Parse.Cloud.define("Student_BatchSaveCards", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var projectId = request.params.projectId;
	var Project = Parse.Object.extend("Project");
	var pointerProject = new Project();
	pointerProject.id = projectId;

    var cards = request.params.cards;
    var langs = request.params.langs;
    var files = request.params.files;

	var ident = "  ###";

	// get "studentProject"
    var studentProjectId = request.params.studentProjectId;
	var querySubmittedProject = new Parse.Query("StudentProject");
	querySubmittedProject.equalTo("student", user);
	querySubmittedProject.equalTo("objectId", studentProjectId);
	querySubmittedProject.include("project.course");
	querySubmittedProject.find({
		success: function(studentProjectResult) {
			//console.log("********* : " + studentProjectResult.length);
			var studentProject = studentProjectResult[0];
			var relation = studentProject.relation("cards");

			var oldCardsId = [];	// old version cards' object id
			console.log(ident);
			for (cardId in cards) {	// I need to fetch these card from parse server, and update them
				console.log(ident + ": " + cardId + "-----------");
				var langKeys = cards[cardId]["langs"].split(",");

				//console.log(ident + ": langkey length " + langKeys.length);

				cards[cardId]["langs"] = {};
				if (langKeys.length == 1) {
						cards[cardId]["langs"][langKeys] = langs[cardId + "-" + langKeys];
				} else {
					console.log(ident + ": size > 1");
					langKeys.forEach(function(langKey) {
						console.log(ident + ": loop " + langKey + "-----------");
						cards[cardId]["langs"][langKey] = langs[cardId + "-" + langKey];
					});
				}

				oldCardsId.push (cardId);
			}

			var onlineCardsQuery = relation.query();
			var keys = ["objectId", "lastUpdatedAt", "imageLastUpdatedAt", "cardId", "image", "isOld", "isDeleted", "status", "comments"];
			onlineCardsQuery.select(keys);
			onlineCardsQuery.equalTo("isOld", 0);
			onlineCardsQuery.addAscending("updatedAt");
			onlineCardsQuery.containedIn("cardId", oldCardsId);	// fetch these cards
			onlineCardsQuery.find({
				success: function(onlineCardsRs) {
					var onlineCards = {};
					//console.log("onlineCardsRs.length " + onlineCardsRs.length);
					for (var i = 0; i < onlineCardsRs.length; i++) { 	// enable to get card by carId later
						var onlineCard = onlineCardsRs[i];
						var cardId = onlineCard.get("cardId");
						onlineCards[cardId] = onlineCard;
					}

					// fetch the langs files
					var langsQuery = GetQuery_Langs();
					langsQuery.containedIn("cardId", oldCardsId);	// fetch these cards
					langsQuery.find({
						success: function(langsRs) {
							var pendingToSaveCards = [];
							var pendingToSaveCardLangs = [];			// card langs have to be saved before cards
							var onlineCardsLangs = {};			// storing the online langs for existing cards
							for (var i = 0; i < langsRs.length; i++) { 	// enable to get card by carId later
								var lang = langsRs[i];
								var cardId = lang.get("cardId");

								if (onlineCardsLangs[cardId] == null || onlineCardsLangs[cardId] === undefined) {
									onlineCardsLangs[cardId] = {};
								}

								var langKey = lang.get("langKey");
								onlineCardsLangs[cardId][langKey] = lang;

								// remove those not use anymore
								if (cards[cardId]["langs"] != null && !(cards[cardId]["langs"] === undefined)) {
									var found = false;
									for (uploadLangKey in cards[cardId]["langs"]) {
										if (langKey === uploadLangKey) {
											found = true;
										}
									}
									if (!found) {
										lang.set("isDeleted", 1);
										pendingToSaveCardLangs.push(lang);
									}
								}
							}

							////////////////////////////////////////////////
							// all the online info fetched
							// now processing them
							// update + save

							var cardAndCardLangLink = [];

							for (cardId in cards) {		// loop through the uploaded card, replace the "onlineCards" with uploaded cards
								cardAndCardLangLink[cardId] = new Array();

								var cardId = cards[cardId]["cardId"];
								//var objectId = cards[cardId]["objectId"];	// the existing card objectId (old)

								var isDeleted = parseInt(cards[cardId]["isDeleted"]);
								var lastUpdatedAt = parseInt(cards[cardId]["lastUpdatedAt"]);
								var imageLastUpdatedAt = parseInt(cards[cardId]["imageLastUpdatedAt"]);

								var CardClass = Parse.Object.extend("Card");
								var card = new CardClass();
								card.set("cardId", cardId);
								card.set("isDeleted", isDeleted);
								card.set("lastUpdatedAt", lastUpdatedAt);
								card.set("imageLastUpdatedAt", imageLastUpdatedAt);
								card.set("status", 0);

								card.set("author", user);
								card.set("project", pointerProject);
								card.set("studentProject", studentProject);

								var imageFile = files[cardId + ".jpg"];

								// copy from oldVersion?
								var oldCard = onlineCards[cardId];
								if (!oldCard) {
									// creating new card
								} else {
									// updating an existing card
									if (oldCard.status == 1 || oldCard.status == 3) {
										console.log("Try to update a passed / featured card");
										// this card should not be updated by student
										continue;
									}

									oldCard.set("isOld", 1);
									card.set("oldVersion", oldCard);	// set the "oldVersion" of new card

									// is new image provided?
									if (!imageFile || imageFile == null) {
										//console.log("use old image");
										imageFile = oldCard.get("image");
									}
									pendingToSaveCards.push(oldCard);
								}

								if (!isDeleted) {
									card.set("image", imageFile);
									//console.log("File: " + imageFile);
								}

								pendingToSaveCards.push(card);
								//////////////////////////////////////
								// langs relation handling (currently in the loop of new Cards


								// langs
								console.log(ident + " " + cardId + "  cards.lang.length " + jsonLength(cards[cardId]["langs"]));

								for (langKey in cards[cardId]["langs"]) {
									console.log(" handling card lang: " + langKey);
									var lang = cards[cardId]["langs"][langKey];
									var name = lang["name"]
									var soundLastUpdatedAt = parseInt(lang["soundLastUpdatedAt"]);

									// update?
									// all langs of these online cards

									var matchingLang = null;
									if (onlineCardsLangs[cardId] != null
									&& !(onlineCardsLangs[cardId] === undefined )
									&& onlineCardsLangs[cardId][langKey] != null) {
										//console.log("  " + langKey + ": 1 exist");
										//console.log("  " + langKey + ": 2 exist");
										//console.log("  " + langKey + ": 3 exist");
										matchingLang = onlineCardsLangs[cardId][langKey];

									} else {
										//console.log("  " + langKey + ": 1 online card langs is null");
										//console.log("  " + langKey + ": 2 online card langs is null");
										//console.log("  " + langKey + ": 3 online card langs is null");
									}

									var CardLangClass = Parse.Object.extend("CardLang");
									var soundFile = files[cardId + "-" + langKey + ".wav"];
									// if file is provided, must be changed
									if (matchingLang == null) {
										console.log(ident + ":  " + langKey + " create new directly");
										// create new directly
										var cardLang = new CardLangClass();
										cardLang.set("langKey", langKey);
										cardLang.set("cardId", cardId);
										cardLang.set("name", name);
										cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);


										cardLang.set("sound", soundFile);

										pendingToSaveCardLangs.push(cardLang);
										cardAndCardLangLink[cardId].push(cardLang);
									} else {
										//console.log(ident + ":  " + langKey + " 1 may be use the old one");
										//console.log(ident + ":  " + langKey + " 2 may be use the old one");
										//console.log(ident + ":  " + langKey + " 3 may be use the old one");

										var cardLang = new CardLangClass();
										cardLang.set("langKey", langKey);
										cardLang.set("cardId", cardId);
										cardLang.set("name", name);
										//cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);

										// anything change?
										var change = 0;

										if (name === matchingLang.get("name")) {
											// no need to update name
										} else {
											change++;
											console.log(ident + ":  " + langKey + " name changed");
										}

										if (soundLastUpdatedAt > matchingLang.get("soundLastUpdatedAt")) {
											change++;
											cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);
											cardLang.set("sound", soundFile);
											console.log(ident + ":  " + langKey + " sound changed");
										} else {
											soundFile = matchingLang.get("sound");
											cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);
											cardLang.set("sound", soundFile);
										}

										console.log("   soundLastUpdatedAt " + soundLastUpdatedAt);

										if (change > 0) {
											// save the new lang
											pendingToSaveCardLangs.push(cardLang);
											cardAndCardLangLink[cardId].push(cardLang);

											matchingLang.set("isOld", 1);
											pendingToSaveCardLangs.push(matchingLang);
											//console.log(ident + ":  " + langKey + " 1 confirm use the new one");
											//console.log(ident + ":  " + langKey + " 2 confirm use the new one");
											//console.log(ident + ":  " + langKey + " 3 confirm use the new one");
										} else {
											// do not need to save new lang
											pendingToSaveCardLangs.push(matchingLang);
											cardAndCardLangLink[cardId].push(matchingLang);
											//console.log(ident + ":  " + langKey + " 1 confirm use the old one");
											//console.log(ident + ":  " + langKey + " 2 confirm use the old one");
											//console.log(ident + ":  " + langKey + " 3 confirm use the old one");
										}
									}
								}
							}

							//response.success();
							// save the langs first, because they are relation of cards
							Parse.Object.saveAll(pendingToSaveCardLangs, {
								success: function(savedLangs) {
									for (var i = 0; i < savedLangs.length; i++) {
										var lang = savedLangs[i];
										var belongToCardId = lang.get("cardId");
										//console.log("belongToCardId: " + belongToCardId);
										var isOld = lang.get("isOld");
										var isDeleted = lang.get("isDeleted");
										if (isOld == 1 || isDeleted == 1)
											continue;

										for (var k = 0;  k < pendingToSaveCards.length; k++) {
											var card = pendingToSaveCards[k];
											var cardId = card.get("cardId");
											var isOld = card.get("isOld");
											var isDeleted = card.get("isDeleted");
											if (isOld == 1 || isDeleted == 1)
												continue;
											if (cardId === belongToCardId) {
												console.log("link: " + cardId);
												card.relation("langs").add(lang);
												break;
											}
										};
									}

									Parse.Object.saveAll(pendingToSaveCards, {
										success: function(saveList) {
											for (var i = 0; i < saveList.length; i++) {
												var card = saveList[i];
												relation.add(card);
											}
											studentProject.save(null, {
											  success: function(studentProject) {

													var keys = ["objectId", "lastUpdatedAt", "imageLastUpdatedAt", "cardId", "image","isOld", "isDeleted", "status", "comments"];
													var query = relation.query();
													query.select(keys);
													query.equalTo("isOld", 0);
													query.find({
														success: function(cards) {

															var cardIds = new Array();
															GetAllCardId(cardIds, cards);
															var langsQuery = GetQuery_Langs();
															langsQuery.containedIn("cardId", cardIds);	// fetch these cards
															langsQuery.find({
																success: function(langsRs) {
																	response.success("{\"success\":" + NO_ERROR +
																			",\"currentTime\":\"" + currentTime + "\"" +
																			",\"user\":" + PreprocessUser(user) +
																			",\"studentProject\":" + JSON.stringify(studentProject) +
																			",\"cards\":" + PreprocessStudentProjectCards(cards) +
																			",\"langs\":" + JSON.stringify(langsRs) +
																			"}");
																}
															});
														}
													});
											  },
											  error: function(error) {
													response.error(error)
											  }
											});
										},
										error: function(error) {
											response.error(error)
										}
									});

								}
							});


							// end of processing
							///////////////////////////////////////////////////
						}
					});









				}
			});

		},
		error: function(error) {
			response.error(error);
		}
	});

});

////////////////////////////////////////////////////////////////////////////////////
// Teacher
Parse.Cloud.define("TeacherGet_SpecificProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var identity = user.get("identity");

	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var courseId = request.params.courseId;
	var Course = Parse.Object.extend("Course");
	var pointerCourse = new Course();
	pointerCourse.id = courseId;
	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("studentOfCourse", pointerCourse);
	queryUser.find({
		success: function(studentResults) {

			var projectId = request.params.projectId;
			var Project = Parse.Object.extend("Project");
			var pointerProject = new Project();
			pointerProject.id = projectId;


			var queryFeaturedCard = GetQuery_FeaturedCards();
			queryFeaturedCard.equalTo("project", pointerProject);
			queryFeaturedCard.include("author");
			queryFeaturedCard.find({
				success: function(featuredCards) {
					// handle featured cards' student data
					var projectIds = [projectId];
					var jsonFeaturedCards = PreprocessFeaturedCards(projectIds, featuredCards);

					var cardIds = new Array();
					GetAllCardId(cardIds, featuredCards);

					var langsQuery = GetQuery_Langs();
					langsQuery.containedIn("cardId", cardIds);	// fetch these cards
					langsQuery.find({
						success: function(langsRs) {

							var querySubmittedProject = new Parse.Query("StudentProject");
							querySubmittedProject.equalTo("project", pointerProject);
							querySubmittedProject.include("student");
							querySubmittedProject.greaterThan("numOfSubmittedCard", 0);
							querySubmittedProject.find({
								success: function(projectSubmittedResults) {

									var json = {};
									if (projectSubmittedResults.length > 0) {

										for (var i = 0; i < projectSubmittedResults.length; i++) {

											var projectSubmittedObj = projectSubmittedResults[i];
											var objectId = projectSubmittedObj.id;

											var userObj = projectSubmittedObj.get("student");
											if (userObj == null)
												continue;
											var userNode = userObj.toJSON();
											var studentId = userObj.id;

											var projectSubmittedNode = (projectSubmittedObj.toJSON());

											json[studentId] = projectSubmittedNode;
											json[studentId]["student"] = userNode;

										}
									}

									response.success("{\"success\":" + NO_ERROR +
											",\"currentTime\":\"" + currentTime + "\"" +
											",\"user\":" + PreprocessUser(user) +
											",\"students\":" + JSON.stringify(studentResults) +
											",\"studentProject\":" + JSON.stringify(json) +
											",\"featuredCards\":" + JSON.stringify(jsonFeaturedCards) +
											",\"langs\":" + JSON.stringify(langsRs) +
											"}");

								},
								error: function(error) {
									response.error(error);
								}
							});
						}
					});
				}
			});
		},
		error: function() {
			response.error("queryUser error");
		}
	});

});

Parse.Cloud.define("TeacherGetSpecificStudent_StudentProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var identity = user.get("identity");

	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var studentId = request.params.studentId;

	var queryStudent = new Parse.Query(Parse.User);
	queryStudent.equalTo("objectId", studentId);
	queryStudent.first({
		success: function(student) {

			var querySubmittedProject = new Parse.Query("StudentProject");
			querySubmittedProject.equalTo("student", student);
			querySubmittedProject.greaterThan("numOfSubmittedCard", 0);
			querySubmittedProject.find({
				success: function(studentProjects) {
					response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"user\":" + PreprocessUser(user) +
							",\"student\":" + JSON.stringify(student) +
							",\"studentProject\":" + JSON.stringify(studentProjects) +
							"}");
				},
				error: function(error) {
					response.error(error);
				}
			});

		},
		error: function(error) {
			response.error(error);
		}
	});



});

// need to modify "savecomment" also
Parse.Cloud.define("Teacher_Get_SpecificStudentProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var identity = user.get("identity");

	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var studentProjectId = request.params.studentProjectId;

	var querySubmittedProject = new Parse.Query("StudentProject");
	querySubmittedProject.equalTo("objectId", studentProjectId);
	querySubmittedProject.include("student");
	querySubmittedProject.include("project");
	querySubmittedProject.include("project.course");
	querySubmittedProject.greaterThan("numOfSubmittedCard", 0);
	querySubmittedProject.first({
		success: function(studentProject) {
			if (studentProject != null) {
				// student info
				var json = {};
				var projectSubmittedObj = studentProject;
				var userObj = projectSubmittedObj.get("student");
				var userNode = userObj.toJSON();

				var projectSubmittedNode = projectSubmittedObj.toJSON();
				projectSubmittedNode["student"] = userNode;

				// project info
				json = {};
				var projectObj = projectSubmittedObj.get("project");
				var projectNode = projectObj.toJSON();

				projectSubmittedNode["project"] = projectNode;

				var courseObj = projectObj.get("course");
				var courseNode = courseObj.toJSON();


				var query = GetQuery_StudentProjectCards(studentProject);
				query.equalTo("isDeleted", 0);
				query.find({
					success: function(cards) {
						var cardIds = new Array();
						GetAllCardId(cardIds, cards);

						var langsQuery = GetQuery_Langs();
						langsQuery.containedIn("cardId", cardIds);	// fetch these cards
						langsQuery.find({
							success: function(langsRs) {
								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										",\"studentProject\":" + JSON.stringify(projectSubmittedNode) +
										",\"course\":" + JSON.stringify(courseNode) +
										",\"cards\":" + PreprocessStudentProjectCards(cards) +
										",\"langs\":" + JSON.stringify(langsRs) +
										"}");
							}
						});
					}
				});
			} else {
				response.success("{\"success\":" + NO_ERROR +
						",\"currentTime\":\"" + currentTime + "\"" +
						",\"user\":" + PreprocessUser(user) +
						"}");
			}
		},
		error: function(error) {
			response.error(error);
		}
	});

});


Parse.Cloud.define("Teacher_BatchSaveCardsComment", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");
	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var cards = request.params.cards;

    var pendingToSaves = [];
	var cardObjectIds = [];
	for (objectId in cards) {
		var status = parseInt(cards[objectId]["status"]);

		var comments = cards[objectId]["comments"];
		var commentsArray = [];
		if (!comments || comments == "") {

		} else {
			commentsArray = comments.split(",");
		}

		var CardClass = Parse.Object.extend("Card");
		var card = new CardClass();

		card.id = objectId;
		card.set("status", status);
		if (commentsArray.length > 0) {
			card.set("comments", commentsArray);
		} else {
			card.set("comments", []);
		}
		card.set("lastUpdatedAt", currentTime);

		pendingToSaves.push(card);
		cardObjectIds.push(objectId);
	}

	Parse.Object.saveAll(pendingToSaves, {
		success: function(saveList) {
			var studentProjectId = request.params.studentProjectId;

			// save the studentProject
			var StudentProject = Parse.Object.extend("StudentProject");
			var pointerStudentProject = new StudentProject();
			pointerStudentProject.id = studentProjectId;
			pointerStudentProject.save(null,{
					success: function(object) {

						var queryUpdatedCard = new Parse.Query("Card");
						queryUpdatedCard.containedIn("objectId", cardObjectIds);
						queryUpdatedCard.find({
							success: function(updatedCards) {
								var projects = [];
								var projectIds = [];
								for (var i = 0; i < updatedCards.length; i++) {
									var updatedCard = updatedCards[i];
									var p = updatedCard.get("project");
									projects.push (p);
									projectIds.push (p.id);
								}

								var queryFeaturedCard = GetQuery_FeaturedCards();
								queryFeaturedCard.containedIn("project", projects);
								queryFeaturedCard.include("author");
								queryFeaturedCard.find({
									success: function(featuredCards) {
										// handle featured cards' student data
										var jsonFeaturedCards = PreprocessFeaturedCards(projectIds, featuredCards);

										var cardIds = new Array();
										GetAllCardId(cardIds, featuredCards);

										var langsQuery = GetQuery_Langs();
										langsQuery.containedIn("cardId", cardIds);	// fetch these cards
										langsQuery.find({
											success: function(langsRs) {
												response.success("{\"success\":" + NO_ERROR +
														",\"currentTime\":\"" + currentTime + "\"" +
														",\"featuredCards\":" + JSON.stringify(jsonFeaturedCards) +
														",\"langs\":" + JSON.stringify(langsRs) +
														"}");
											}
										});
									},
									error: function(error) {
										response.error(error);
									}
								});
							},
							error: function(error) {
								response.error(error);
							}
						});

					},
					error: function(error) {
						response.error(error);
					}
				});

			},
			error: function(error) {
				response.error(error)
			}
	});
});

Parse.Cloud.define("Teacher_AddCard", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);

    var user = request.user;
	var identity = user.get("identity");
	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var projectId = request.params.projectId;
	var Project = Parse.Object.extend("Project");
	var pointerProject = new Project();
	pointerProject.id = projectId;

    var cardData = request.params.card;
    var langs = request.params.langs;
    var files = request.params.files;

	// read the uploaded card
	var cardId = cardData["cardId"];
	var langKeys = cardData["langs"].split(",");

	cardData["langs"] = {};

	if (langKeys.length == 1) {
		console.log("langkey == 1 " + langKeys);
		console.log("langkey == 1 " + langKeys);
		console.log("langkey == 1 " + langKeys);
		cardData["langs"][langKeys] = langs[cardId + "-" + langKeys];
	} else {
		console.log(": size > 1");
		console.log(": size > 1");
		console.log(": size > 1");
		langKeys.forEach(function(langKey) {
			console.log("Received " + langKey);
			console.log("Received " + langKey);
			console.log("Received " + langKey);
			console.log("Received " + langKey);
			cardData["langs"][langKey] = langs[cardId + "-" + langKey];
		});
	}

	var oldquery = new Parse.Query("Card");
	var keys = ["objectId", "lastUpdatedAt", "imageLastUpdatedAt", "cardId", "image", "isOld", "isDeleted", "status", "comments"];
	oldquery.select(keys);
	oldquery.equalTo("isOld", 0);
	oldquery.equalTo("cardId", cardId);
	oldquery.equalTo("author", user);
	oldquery.addAscending("updatedAt");
	oldquery.first({
		success: function(oldCard) {	// should be only one card
			var oldCardId = "";
			if (oldCard != null) {
				oldCardId = oldCard.get("cardId");
			}

			var langsQuery = GetQuery_Langs();
			langsQuery.equalTo("cardId", oldCardId);	// fetch these cards
			langsQuery.find({
				success: function(langsRs) {

					var pendingToSaveCards = [];
					var pendingToSaveCardLangs = [];

					var onlineCardLangs = {};			// storing the online langs for existing cards
					for (var i = 0; i < langsRs.length; i++) { 	// enable to get card by carId later
						var lang = langsRs[i];

						var langKey = lang.get("langKey");
						onlineCardLangs[langKey] = lang;

						console.log("--------------------");
						console.log("--------------------");
						console.log("db " + langKey);
						console.log("db " + langKey);

						// remove those not use anymore
						if (cardData["langs"] != null && !(cardData["langs"] === undefined)) {
							var found = false;
							for (uploadLangKey in cardData["langs"]) {
								if (langKey === uploadLangKey) {
									console.log("found " + langKey);
									console.log("found " + langKey);
									found = true;
								}
							}
							if (!found) {
								console.log("remove " + langKey);
								console.log("remove " + langKey);
								lang.set("isDeleted", 1);
								pendingToSaveCardLangs.push(lang);
							}
						}
					}

					////////////////////////////////////////////////
					// all the online info fetched
					// now processing them
					// update + save

					var isDeleted = parseInt(cardData["isDeleted"]);
					var lastUpdatedAt = parseInt(cardData["lastUpdatedAt"]);
					var imageLastUpdatedAt = parseInt(cardData["imageLastUpdatedAt"]);

					var CardClass = Parse.Object.extend("Card");
					var card = new CardClass();
					card.set("cardId", cardId);
					card.set("isDeleted", isDeleted);
					card.set("lastUpdatedAt", lastUpdatedAt);
					card.set("imageLastUpdatedAt", imageLastUpdatedAt);

					card.set("author", user);
					card.set("project", pointerProject);
					card.set("status", 3);

					var imageFile = files["featured-" + cardId + ".jpg"];

					// copy from oldVersion?
					if (!oldCard) {
						// creating new card
					} else {
						oldCard.set("isOld", 1);
						card.set("oldVersion", oldCard);	// set the "oldVersion" of new card

						// is new image provided?
						if (!imageFile || imageFile == null) {
							//console.log("use old image");
							imageFile = oldCard.get("image");
						}
						pendingToSaveCards.push(oldCard);
					}
					console.log(" p 4");
					if (!isDeleted) {
						card.set("image", imageFile);
					}
					pendingToSaveCards.push(card);

					//////////////////////////////////////
					// langs relation handling
					for (langKey in cardData["langs"]) {
						console.log(" Handling lang " + langKey);
						console.log(" Handling lang " + langKey);
						console.log(" Handling lang " + langKey);

						var lang = cardData["langs"][langKey];
						var name = lang["name"]
						var soundLastUpdatedAt = parseInt(lang["soundLastUpdatedAt"]);

						// update?
						// all langs of online cards
						var matchingLang = null;
						if (onlineCardLangs != null
						&& !(onlineCardLangs === undefined )
						&& onlineCardLangs[langKey] != null) {
							matchingLang = onlineCardLangs[langKey];
						}

						var CardLangClass = Parse.Object.extend("CardLang");
						var soundFile = files["featured-" + cardId + "-" + langKey + ".wav"];
						// if file is provided, must be changed
						if (matchingLang == null) {
							//console.log(ident + ":  " + langKey + " create new directly");
							// create new directly
							var cardLang = new CardLangClass();
							cardLang.set("langKey", langKey);
							cardLang.set("cardId", cardId);
							cardLang.set("name", name);
							cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);

							cardLang.set("sound", soundFile);

							pendingToSaveCardLangs.push(cardLang);
							//cardAndCardLangLink[cardId].push(cardLang);
						} else {
							//console.log(ident + ":  " + langKey + " 1 may be use the old one");
							//console.log(ident + ":  " + langKey + " 2 may be use the old one");
							//console.log(ident + ":  " + langKey + " 3 may be use the old one");

							var cardLang = new CardLangClass();
							cardLang.set("langKey", langKey);
							cardLang.set("cardId", cardId);
							cardLang.set("name", name);

							// anything change?
							var change = 0;

							if (name === matchingLang.get("name")) {
								// no need to update name
							} else {
								change++;
								//console.log(ident + ":  " + langKey + " name changed");
							}

							if (soundLastUpdatedAt > matchingLang.get("soundLastUpdatedAt")) {
								change++;
								cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);
								cardLang.set("sound", soundFile);
								//console.log(ident + ":  " + langKey + " sound changed");
							} else {
								soundFile = matchingLang.get("sound");
								cardLang.set("soundLastUpdatedAt", soundLastUpdatedAt);
								cardLang.set("sound", soundFile);
							}

							//console.log("   soundLastUpdatedAt " + soundLastUpdatedAt);

							if (change > 0) {
								// save the new lang
								pendingToSaveCardLangs.push(cardLang);

								matchingLang.set("isOld", 1);
								pendingToSaveCardLangs.push(matchingLang);
								//console.log(ident + ":  " + langKey + " 1 confirm use the new one");
								//console.log(ident + ":  " + langKey + " 2 confirm use the new one");
								//console.log(ident + ":  " + langKey + " 3 confirm use the new one");
							} else {
								// do not need to save new lang
								pendingToSaveCardLangs.push(matchingLang);
								//cardAndCardLangLink[cardId].push(matchingLang);
								//console.log(ident + ":  " + langKey + " 1 confirm use the old one");
								//console.log(ident + ":  " + langKey + " 2 confirm use the old one");
								//console.log(ident + ":  " + langKey + " 3 confirm use the old one");
							}
						}
					}
							//console.log(" p 5");
							//console.log(" p 5");
							//console.log(" p 5");
					// save the langs first, because they are relation of cards
					Parse.Object.saveAll(pendingToSaveCardLangs, {
						success: function(savedLangs) {
							//console.log(" p 6");
							//console.log(" p 6");
							//console.log(" p 6");
							for (var i = 0; i < savedLangs.length; i++) {
								var lang = savedLangs[i];
								var belongToCardId = lang.get("cardId");
								//console.log("belongToCardId: " + belongToCardId);
								var isOld = lang.get("isOld");
								var isDeleted = lang.get("isDeleted");
								if (isOld == 1 || isDeleted == 1)
									continue;

								for (var k = 0;  k < pendingToSaveCards.length; k++) {
									var toBeSaveCard = pendingToSaveCards[k];
									var cardId = toBeSaveCard.get("cardId");
									var isOld = toBeSaveCard.get("isOld");
									var isDeleted = toBeSaveCard.get("isDeleted");
									if (isOld == 1 || isDeleted == 1)
										continue;
									if (cardId === belongToCardId) {
										//console.log("link: " + cardId);
										toBeSaveCard.relation("langs").add(lang);
										break;
									}
								};
							}
							console.log(" p 7");
							console.log(" p 7");
							console.log(" p 7");
							Parse.Object.saveAll(pendingToSaveCards, {
								success: function(saveList) {
									console.log(" p 8");
									response.success("{\"success\":" + NO_ERROR +
													",\"currentTime\":\"" + currentTime + "\"" +
													",\"user\":" + PreprocessUser(user) +
													"}");
								},
								error: function(error) {
									response.error(error)
								}
							});

						}
					});
				}
			});

			/*

			//card.set("studentProject", studentProject);

			var imageFile = image;
			var soundFile = sound;

			// oldVersion?
			// old card
			if (!oldCard) {
				// this is new
			} else {
				oldCard.set("isOld", 1);
				card.set("oldVersion", oldCard);

				if (!imageFile || imageFile == null) {
					imageFile = oldCard.get("image");
				}
				if (!soundFile || soundFile == null) {
					soundFile = oldCard.get("sound");
				}
				pendingToSaves.push(oldCard);
			}

			if (!isDeleted) {
				card.set("image", imageFile);
				card.set("sound", soundFile);
			}
			pendingToSaves.push(card);

			Parse.Object.saveAll(pendingToSaves, {
				success: function(saveList) {
					response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"user\":" + PreprocessUser(user) +
									"}");
				},
				error: function(error) {
					response.error(error)
				}
			});
			*/
		}
	});

});

Parse.Cloud.define("Teacher_RemoveCardFromFeatured", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);

    var user = request.user;
	var identity = user.get("identity");
	if (identity <= 1) {
		response.error(ERROR_PERMISSION);
		return;
	}

    var objectId = request.params.objectId;

	var query = new Parse.Query("Card");
	query.equalTo("objectId", objectId);
	query.include("author");
	query.first({
		success: function(oldCard) {
			if (!oldCard) {
				response.error("no card");
			} else {
				// copy as a new card

				oldCard.set("status", 1);
				//oldCard.set("isDeleted", 1);	// old one is deleted, for fetching deleted featured card
				//oldCard.set("isOld", 1);	// old one is deleted, status stull = 3, for fetching deleted featured card
				oldCard.save(null, {
					success: function(saveList) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error)
					}
				});
			}

		}
	});

});

















///////////////////////////////////////////////////////////////////////////////////////
// Web

Parse.Cloud.define("Web_Get_Home", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2) {
		// teacher
		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("courseTeacher", user);
		queryCourse.descending("createdAt");
		queryCourse.find({
			success: function(courseResults) {
				if (courseResults.length > 0) {
					// also find the project
					var queryProject = new Parse.Query("Project");
					queryProject.containedIn("course", courseResults);
					queryProject.equalTo("isDeleted", 0);
					queryProject.descending("dueDate");
					queryProject.find({
						success: function(projectResults) {
							response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"courses\":" + CoursesHandle(courseResults) +
								",\"my-projects\":" + ProjectsHandle(projectResults) +
								",\"user\":" + PreprocessUser(user) +
								"}");


						},
						error: function() {
							response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"courses\":" + CoursesHandle(courseResults) +
								",\"my-projects\": {}"  +
								",\"user\":" + PreprocessUser(user) +
								"}");
						}
					});
				} else {
				   response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
								",\"courses\": {}"  +
							",\"user\":" + PreprocessUser(user) +
							"}");

				}
			},
			error: function() {
				response.error("Unknown error");
			}
		});
	} else if (identity == 3) {
		// school admin
		var query = new Parse.Query("School");
		query.equalTo("objectId", user.get("school").id);
		query.ascending("abbreviation");
		query.first({
			success: function(school) {

				var schoolNode = school.toJSON();
				delete schoolNode["updatedAt"];
				delete schoolNode["createdAt"];

				var queryTeacher = new Parse.Query(Parse.User);
				queryTeacher.equalTo("school", school);
				queryTeacher.equalTo("identity", 2);

				var querySchoolAdmin = new Parse.Query(Parse.User);
				querySchoolAdmin.equalTo("school", school);
				querySchoolAdmin.equalTo("identity", 3);

				var queryCombine = Parse.Query.or(queryTeacher, querySchoolAdmin);

				queryCombine.find({
					success: function(teacherResults) {
						var queryCourse = new Parse.Query("Course");
						queryCourse.equalTo("school", school);
						queryCourse.find({
							success: function(courseResults) {

								// get studentProjects of these project


								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"school\":" + JSON.stringify(schoolNode) +
										",\"courses\":" + JSON.stringify(courseResults) +
										",\"teachers\":" + JSON.stringify(teacherResults) +
										",\"user\":" + PreprocessUser(user) +
										"}");
							}
						});
					}
				});

			}
		});
	} else if (identity == 4) {
		// system admin
		var querySchool = new Parse.Query("School");
		querySchool.descending("createdAt");
		querySchool.find({
			success: function(schoolResults) {
				if (schoolResults.length > 0) {
					response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"schools\":" + SchoolsHandle(schoolResults) +
								",\"user\":" + PreprocessUser(user) +
								"}");
				} else {
				   response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"schools\": {}"  +
							",\"user\":" + PreprocessUser(user) +
							"}");

				}
			},
			error: function() {
				response.error("Unknown error");
			}
		});
	} else {
		response.error("Unknown identity");
	}
});

Parse.Cloud.define("Web_Get_Course", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var courseId = request.params.courseId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher
		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("objectId", courseId);
		queryCourse.include("school");
		queryCourse.descending("createdAt");
		queryCourse.first({
			success: function(course) {

				var courseNode = course.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				var school = course.get("school");

				var queryUser = new Parse.Query(Parse.User);
				queryUser.equalTo("studentOfCourse", course);
				queryUser.find({
					success: function(studentResults) {
						var queryProject = new Parse.Query("Project");
						queryProject.equalTo("isDeleted", 0);
						queryProject.equalTo("course", course);
						queryProject.find({
							success: function(projectResults) {

								// get studentProjects of these project
								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"course\":" + JSON.stringify(courseNode) +
										",\"school\":" + JSON.stringify(school) +
										",\"projects\":" + ProjectsHandle(projectResults) +
										",\"students\":" + WebHandleStudents(studentResults) +
										",\"user\":" + PreprocessUser(user) +
										"}");
							}
						});
					}
				});
			},
			error: function(error) {
				response.error(error);
			}
		});
	} else {
		response.error("Unknown identity");
	}
});


Parse.Cloud.define("Web_Add_User_Batch", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var courseId = request.params.courseId;
    var year = parseInt(request.params.year);
    var studentNames = request.params.studentNames;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher
		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("objectId", courseId);
		queryCourse.include("school");
		queryCourse.descending("createdAt");
		queryCourse.first({
			success: function(course) {

				var courseNode = course.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				var school = course.get("school");

				var querySchoolYear = new Parse.Query("SchoolYear");
				querySchoolYear.equalTo("school", school);
				querySchoolYear.equalTo("year", year);
				querySchoolYear.find({
					success: function(schoolYearRs) {
						var schoolYear = null;
						var yearKey = year % 1000;		// 2015 -> 15
						if (schoolYearRs.length == 0) {
							// create new !!!
							var SchoolYearClass = Parse.Object.extend("SchoolYear");
							schoolYear = new SchoolYearClass();
							schoolYear.set("school", school);
							schoolYear.set("year", year);
							schoolYear.set("lastStudentId", yearKey + "000");
						} else {
							// get the last one
							var schoolYear = schoolYearRs[0];
						}
						// key
						var yearKey = year % 1000;
						var lastStudentId = parseInt(schoolYear.get("lastStudentId"));

						var abbr = school.get("abbreviation");

						var pendingToSaves = [];

						var currentId = lastStudentId;
						studentNames.forEach(function(name) {
							currentId++;
							var studentId = currentId;
							console.log(studentId + " - " + name);
							console.log(studentId + " - " + name);
							var password = generatePassword();
							//console.log(studentId + ": " + name + " - " + generatePassword());
							var newUser = new Parse.User();
							newUser.set("username", abbr + "." + studentId);
							newUser.set("realName", name);
							newUser.set("password", password);
							newUser.set("defaultPassword", password);
							newUser.set("identity", 1);
							newUser.set("studentOfCourse", course);
							newUser.set("school", school);

							//newUser.save();
							newUser.signUp();
						});


						schoolYear.set("lastStudentId", currentId);
						schoolYear.save();

					}
				});
			},
			error: function(error) {
				response.error(error);
			}
		});
	} else {
		response.error("Unknown identity");
	}
});


Parse.Cloud.define("Web_Get_User_Batch", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var courseId = request.params.courseId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher
		var queryCourse = new Parse.Query("Course");
		queryCourse.equalTo("objectId", courseId);
		queryCourse.include("school");
		queryCourse.first({
			success: function(course) {

				var courseNode = course.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				var school = course.get("school");

				var queryUser = new Parse.Query(Parse.User);
				queryUser.equalTo("studentOfCourse", course);
				queryUser.ascending("username");
				queryUser.find({
					success: function(studentResults) {
						response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"students\":" + WebHandleStudents(studentResults) +
							",\"school\":" + JSON.stringify(school) +
							",\"course\":" + JSON.stringify(course) +
							",\"user\":" + PreprocessUser(user) +
							"}");
					}
				});
			},
			error: function(error) {
				response.error(error);
			}
		});
	} else {
		response.error("Unknown identity");
	}
});

function generatePassword() {
    var length = 8,
        charset = "abcdefghknprstuvwxyz",
        retVal = "";
    for (var i = 0, n = charset.length; i < length; ++i) {
        retVal += charset.charAt(Math.floor(Math.random() * n));
    }
    return retVal;
}

function WebHandleStudents(studentResults) {

    var studentsJSON = {};
    for (var i = 0; i < studentResults.length; i++) {

        var obj = studentResults[i];
        var objectId = obj.id;

        var json = (obj.toJSON());
        delete json["createdAt"];
        delete json["studentOfCourse"];
        delete json["updatedAt"];
        delete json["identity"];

		var numOfTeacherAttention = json["numOfTeacherAttention"];
		if (numOfTeacherAttention == 0)
			delete json["numOfTeacherAttention"];

        studentsJSON[objectId] = json;
    }

    return JSON.stringify(studentsJSON);
}

function WebHandleCards(cards, langs) {

    var outputJSON = {};
    for (var i = 0; i < cards.length; i++) {

        var obj = cards[i];
        var objectId = obj.id;
        var cardId = obj.get("cardId");

        var json = (obj.toJSON());
        delete json["createdAt"];
        delete json["updatedAt"];
        delete json["imageLastUpdatedAt"];
        delete json["isDeleted"];
        delete json["isOld"];
        delete json["lastUpdatedAt"];
        delete json["project"];
        delete json["studentProject"];
        delete json["studentAttention"];
        delete json["teacherAttention"];

		var userObj = obj.get("author");
		if (!userObj) {
		} else {
			var userNode = userObj.toJSON();
			delete userNode["identity"];
			delete userNode["numOfTeacherAttention"];
			delete userNode["studentOfCourse"];
			delete userNode["createdAt"];
			delete userNode["updatedAt"];
			json["author"] = userNode;
		}

		var numOfTeacherAttention = json["numOfTeacherAttention"];
		if (numOfTeacherAttention == 0)
			delete json["numOfTeacherAttention"];


        outputJSON[cardId] = json;
		outputJSON[cardId]["langs"] = {};
    }


    for (var i = 0; i < langs.length; i++) {

        var obj = langs[i];
        var objectId = obj.id;
        var cardId = obj.get("cardId");
        var langKey = obj.get("langKey");

        var json = (obj.toJSON());
        delete json["createdAt"];
        delete json["updatedAt"];
        delete json["soundLastUpdatedAt"];
        delete json["isDeleted"];
        delete json["isOld"];
        delete json["sound"]["__type"];
        delete json["sound"]["name"];

        outputJSON[cardId]["langs"][langKey] = json;
    }

    return JSON.stringify(outputJSON);
}

Parse.Cloud.define("Web_Get_Project", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var projectId = request.params.projectId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher

		var queryProject = new Parse.Query("Project");
		queryProject.equalTo("objectId", projectId);
		queryProject.equalTo("isDeleted", 0);3
		queryProject.include("course");
		queryProject.first({
			success: function(project) {
				// get course data
				var courseObj = project.get("course");
				var courseNode = courseObj.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				// get student list
				var queryUser = new Parse.Query(Parse.User);
				queryUser.equalTo("studentOfCourse", courseObj);
				queryUser.find({
					success: function(studentResults) {
						// get studentProject
						var queryStudentProject = new Parse.Query("StudentProject");
						queryStudentProject.equalTo("project", project);
						queryStudentProject.find({
							success: function(studentProjectsResults) {
								// remove unwanted data
								var studentProjectJSON = {};
								if (studentProjectsResults.length > 0) {
									for (var i = 0; i < studentProjectsResults.length; i++) {
										var obj = studentProjectsResults[i];
										var objectId = obj.id;

										var userObj = obj.get("student");
										var userNode = userObj.toJSON();
										var studentId = userObj.id;

										var projectSubmittedNode = (obj.toJSON());
										delete projectSubmittedNode["student"];
										delete projectSubmittedNode["createdAt"];
										delete projectSubmittedNode["updatedAt"];
										delete projectSubmittedNode["project"];
										delete projectSubmittedNode["numOfStudentAttention"];
										delete projectSubmittedNode["cards"];

										var numOfTeacherAttention = projectSubmittedNode["numOfTeacherAttention"];
										if (numOfTeacherAttention == 0) {
											delete projectSubmittedNode["numOfTeacherAttention"];
										}

										studentProjectJSON[studentId] = projectSubmittedNode;
									}
								}

								// featured cards
								var queryFeaturedCard = new Parse.Query("Card");
								queryFeaturedCard.equalTo("project", project);
								queryFeaturedCard.equalTo("status", 3);
								queryFeaturedCard.equalTo("isOld", 0);
								queryFeaturedCard.equalTo("isDeleted", 0);
								queryFeaturedCard.count({
									success: function(numOfFeatured) {
										response.success("{\"success\":" + NO_ERROR +
												",\"currentTime\":\"" + currentTime + "\"" +
												",\"project\":" + JSON.stringify(project) +
												",\"course\":" + JSON.stringify(courseNode) +
												",\"students\":" + WebHandleStudents(studentResults) +
												",\"studentProjects\":" + JSON.stringify(studentProjectJSON) +
												",\"numOfFeatured\":" + numOfFeatured +
												",\"user\":" + PreprocessUser(user) +
												"}");
									}
								});
							}
						});
					}
				});
			}
		});
	} else {
		response.error("Unknown identity");
	}
});

Parse.Cloud.define("Web_Get_Cards", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var projectId = request.params.projectId;
    var cardStatus = request.params.cardStatus;
	var identity = user.get("identity");


	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher
		// specific project
		var queryProject = new Parse.Query("Project");
		queryProject.equalTo("objectId", projectId);
		queryProject.equalTo("isDeleted", 0);
		queryProject.include("course");
		queryProject.first({
			success: function(project) {
				// get course data
				var courseObj = project.get("course");
				var courseNode = courseObj.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				// featured cards
				var allValidCard = new Parse.Query("Card");
				allValidCard.equalTo("status", 1);
				var featuredCard = new Parse.Query("Card");
				featuredCard.equalTo("status", 3);

				var queryCards = null;
				if (cardStatus == 1)
					queryCards = Parse.Query.or(allValidCard, featuredCard);
				else
					queryCards = featuredCard;

				//var queryFeaturedCard = new Parse.Query("Card");
				queryCards.equalTo("project", project);
				queryCards.equalTo("isOld", 0);
				queryCards.equalTo("isDeleted", 0);
				queryCards.include("author");
				queryCards.find({
					success: function(featuredCards) {

						var cardIds = new Array();
						GetAllCardId(cardIds, featuredCards);
						var langsQuery = GetQuery_Langs();
						langsQuery.containedIn("cardId", cardIds);	// fetch these cards
						langsQuery.find({
							success: function(langsRs) {
								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"project\":" + JSON.stringify(project) +
										",\"course\":" + JSON.stringify(courseNode) +
										",\"cards\":" + WebHandleCards(featuredCards, langsRs) +
										",\"user\":" + PreprocessUser(user) +
										"}");
							}
						});
					}
				});
			}
		});
	} else {
		response.error("Unknown identity");
	}
});

Parse.Cloud.define("Web_Get_StudentProject", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var studentProjectId = request.params.studentProjectId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher

		var queryStudentProject = new Parse.Query("StudentProject");
		queryStudentProject.equalTo("objectId", studentProjectId);
		queryStudentProject.include("project");
		queryStudentProject.include("project.course");
		queryStudentProject.include("student");
		queryStudentProject.first({
			success: function(studentProject) {
				var studentProjectNode = studentProject.toJSON();
				delete studentProjectNode["updatedAt"];
				delete studentProjectNode["createdAt"];
				delete studentProjectNode["cards"];
				delete studentProjectNode["numOfStudentAttention"];
				delete studentProjectNode["numOfTeacherAttention"];
				delete studentProjectNode["numOfSubmittedCard"];
				delete studentProjectNode["project"];
				delete studentProjectNode["student"];

				// get project data
				var projectObj = studentProject.get("project");
				var projectNode = projectObj.toJSON();
				delete projectNode["updatedAt"];
				delete projectNode["createdAt"];
				delete projectNode["numOfTeacherAttention"];
				delete projectNode["numOfStudentAttention"];
				delete projectNode["course"];
				delete projectNode["courseIconUpdatedAt"];
				delete projectNode["createdBy"];
				delete projectNode["dueDate"];
				delete projectNode["numOfStudentDone"];
				delete projectNode["numOfTeacherAttention"];
				delete projectNode["projectIcon"];
				delete projectNode["projectDesc"];
				delete projectNode["type"];
				delete projectNode["createdAt"];
				delete projectNode["updatedAt"];

				// get course data
				var courseObj = projectObj.get("course");
				var courseNode = courseObj.toJSON();
				delete courseNode["updatedAt"];
				delete courseNode["createdAt"];
				delete courseNode["numOfTeacherAttention"];
				delete courseNode["courseTeacher"];
				delete courseNode["courseIconUpdatedAt"];
				delete courseNode["courseIcon"];

				// get student data
				var studentObj = studentProject.get("student");
				var studentNode = studentObj.toJSON();
				delete studentNode["updatedAt"];
				delete studentNode["createdAt"];
				delete studentNode["numOfTeacherAttention"];
				delete studentNode["studentOfCourse"];
				delete studentNode["identity"];

				var query = GetQuery_StudentProjectCards(studentProject);
				query.equalTo("isDeleted", 0);
				query.find({
					success: function(cards) {
						var cardIds = new Array();
						GetAllCardId(cardIds, cards);
						var langsQuery = GetQuery_Langs();
						langsQuery.containedIn("cardId", cardIds);	// fetch these cards
						langsQuery.find({
							success: function(langsRs) {
								response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"project\":" + JSON.stringify(projectNode) +
									",\"course\":" + JSON.stringify(courseNode) +
									",\"student\":" + JSON.stringify(studentNode) +
									",\"studentProject\":" + JSON.stringify(studentProject) +
									",\"cards\":" + WebHandleCards(cards, langsRs) +
									",\"user\":" + PreprocessUser(user) +
									"}");
							}
						});
					}
				});

			}
		});
	} else {
		response.error("Unknown identity");
	}
});


Parse.Cloud.define("Web_Put_StudentProject_Comment", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var studentProjectId = request.params.studentProjectId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher

		// save first
		var cards = request.params.cards;
		var pendingToSaves = [];
		var cardObjectIds = [];
		for (objectId in cards) {
			var status = parseInt(cards[objectId]["status"]);

			var comments = cards[objectId]["comments"];
			var commentsArray = [];
			if (!comments || comments == "") {

			} else {
				commentsArray = comments.split(",");
			}

			var CardClass = Parse.Object.extend("Card");
			var card = new CardClass();

			card.id = objectId;
			card.set("status", status);
			if (commentsArray.length > 0) {
				card.set("comments", commentsArray);
			} else {
				card.set("comments", []);
			}
			card.set("lastUpdatedAt", currentTime);

			pendingToSaves.push(card);
			cardObjectIds.push(objectId);
			console.log("save " + objectId);
		}

		// then get
		Parse.Object.saveAll(pendingToSaves, {
			success: function(saveList) {

				var queryStudentProject = new Parse.Query("StudentProject");
				queryStudentProject.equalTo("objectId", studentProjectId);
				queryStudentProject.include("project");
				queryStudentProject.include("project.course");
				queryStudentProject.include("student");
				queryStudentProject.first({
					success: function(studentProject) {
						var studentProjectNode = studentProject.toJSON();
						delete studentProjectNode["updatedAt"];
						delete studentProjectNode["createdAt"];
						delete studentProjectNode["cards"];
						delete studentProjectNode["numOfStudentAttention"];
						delete studentProjectNode["numOfTeacherAttention"];
						delete studentProjectNode["numOfSubmittedCard"];
						delete studentProjectNode["project"];
						delete studentProjectNode["student"];

						// get project data
						var projectObj = studentProject.get("project");
						var projectNode = projectObj.toJSON();
						delete projectNode["updatedAt"];
						delete projectNode["createdAt"];
						delete projectNode["numOfTeacherAttention"];
						delete projectNode["numOfStudentAttention"];
						delete projectNode["course"];
						delete projectNode["courseIconUpdatedAt"];
						delete projectNode["createdBy"];
						delete projectNode["dueDate"];
						delete projectNode["numOfStudentDone"];
						delete projectNode["numOfTeacherAttention"];
						delete projectNode["projectIcon"];
						delete projectNode["projectDesc"];
						delete projectNode["type"];
						delete projectNode["createdAt"];
						delete projectNode["updatedAt"];

						// get course data
						var courseObj = projectObj.get("course");
						var courseNode = courseObj.toJSON();
						delete courseNode["updatedAt"];
						delete courseNode["createdAt"];
						delete courseNode["numOfTeacherAttention"];
						delete courseNode["courseTeacher"];
						delete courseNode["courseIconUpdatedAt"];
						delete courseNode["courseIcon"];

						// get student data
						var studentObj = studentProject.get("student");
						var studentNode = studentObj.toJSON();
						delete studentNode["updatedAt"];
						delete studentNode["createdAt"];
						delete studentNode["numOfTeacherAttention"];
						delete studentNode["studentOfCourse"];
						delete studentNode["identity"];

						var query = GetQuery_StudentProjectCards(studentProject);
						query.equalTo("isDeleted", 0);
						query.find({
							success: function(cards) {
								console.log("555555555");
								console.log("555555555");
								var cardIds = new Array();
								GetAllCardId(cardIds, cards);
								var langsQuery = GetQuery_Langs();
								langsQuery.containedIn("cardId", cardIds);	// fetch these cards
								langsQuery.find({
									success: function(langsRs) {
										response.success("{\"success\":" + NO_ERROR +
											",\"currentTime\":\"" + currentTime + "\"" +
											",\"project\":" + JSON.stringify(projectNode) +
											",\"course\":" + JSON.stringify(courseNode) +
											",\"student\":" + JSON.stringify(studentNode) +
											",\"studentProject\":" + JSON.stringify(studentProject) +
											",\"cards\":" + WebHandleCards(cards, langsRs) +
											",\"user\":" + PreprocessUser(user) +
											"}");
									}
								});
							}
						});

					}
				});

			}
		});

	} else {
		response.error("Unknown identity");
	}
});



Parse.Cloud.define("Web_Get_Student", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var studentId = request.params.studentId;

	var identity = user.get("identity");

	if (identity == 1) {
		response.error("Unauthorized");
	} else if (identity == 2 || identity == 3 || identity == 4) {
		// teacher
		var queryUser = new Parse.Query(Parse.User);
		queryUser.equalTo("objectId", studentId);
		queryUser.first({
			success: function(student) {
				var queryCourse = new Parse.Query("Course");
				queryCourse.equalTo("objectId", student.get("studentOfCourse").id);
				queryCourse.descending("createdAt");
				queryCourse.first({
					success: function(course) {

						var courseNode = course.toJSON();
						delete courseNode["updatedAt"];
						delete courseNode["createdAt"];
						delete courseNode["numOfTeacherAttention"];
						delete courseNode["courseTeacher"];
						delete courseNode["courseIconUpdatedAt"];
						delete courseNode["courseIcon"];

						// project of this course
						var queryProject = new Parse.Query("Project");
						queryProject.equalTo("isDeleted", 0);
						queryProject.equalTo("course", course);
						queryProject.find({
							success: function(projectResults) {
								// student project of this student
								var querySubmittedProject = new Parse.Query("StudentProject");
								querySubmittedProject.equalTo("student", student);
								querySubmittedProject.find({
									success: function(studentProjectsResults) {
										var studentProjectJSON = {};
										if (studentProjectsResults.length > 0) {
											for (var i = 0; i < studentProjectsResults.length; i++) {
												var obj = studentProjectsResults[i];
												var objectId = obj.id;

												var projectObj = obj.get("project");
												var projectId = projectObj.id;

												var projectSubmittedNode = (obj.toJSON());
												delete projectSubmittedNode["student"];
												delete projectSubmittedNode["createdAt"];
												delete projectSubmittedNode["updatedAt"];
												delete projectSubmittedNode["project"];
												delete projectSubmittedNode["numOfStudentAttention"];
												delete projectSubmittedNode["cards"];

												studentProjectJSON[projectId] = projectSubmittedNode;
											}
										}

										response.success("{\"success\":" + NO_ERROR +
												",\"currentTime\":\"" + currentTime + "\"" +
												",\"course\":" + JSON.stringify(courseNode) +
												",\"projects\":" + ProjectsHandle(projectResults) +
												",\"student\":" + JSON.stringify(student) +
												",\"studentProjects\":" + JSON.stringify(studentProjectJSON) +
												",\"user\":" + PreprocessUser(user) +
												"}");

									}
								});
							}
						});
					}
				});

			}
		});
	} else {
		response.error("Unknown identity");
	}
});


Parse.Cloud.define("Web_Get_School", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

    var schoolId = request.params.schoolId;

	var identity = user.get("identity");

	if (identity == 3 || identity == 4) {
		// teacher
		var query = new Parse.Query("School");
		query.equalTo("objectId", schoolId);
		query.descending("createdAt");
		query.first({
			success: function(school) {

				var schoolNode = school.toJSON();
				delete schoolNode["updatedAt"];
				delete schoolNode["createdAt"];

				var queryTeacher = new Parse.Query(Parse.User);
				queryTeacher.equalTo("school", school);
				queryTeacher.equalTo("identity", 2);

				var querySchoolAdmin = new Parse.Query(Parse.User);
				querySchoolAdmin.equalTo("school", school);
				querySchoolAdmin.equalTo("identity", 3);

				var queryCombine = Parse.Query.or(queryTeacher, querySchoolAdmin);

				queryCombine.find({
					success: function(teacherResults) {
						var queryCourse = new Parse.Query("Course");
						queryCourse.equalTo("school", school);
						queryCourse.find({
							success: function(courseResults) {

								// get studentProjects of these project


								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"school\":" + JSON.stringify(schoolNode) +
										",\"courses\":" + JSON.stringify(courseResults) +
										",\"teachers\":" + JSON.stringify(teacherResults) +
										",\"user\":" + PreprocessUser(user) +
										"}");
							}
						});
					}
				});

			}
		});
	} else {
		response.error("Unauthorized");
	}
});

Parse.Cloud.define("Web_Add_User", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var username = request.params.username;
    var realName = request.params.realName;
    var password = request.params.password;
    var email = request.params.email;
    var userIdentity =  parseInt(request.params.identity);

	// checking identity valid
	if (userIdentity >= identity || identity <= 2) {
        response.error("No Permission");
		return;
	}

	// basic info of new user
	var newUser = new Parse.User();
	newUser.set("username", username);
	newUser.set("realName", realName);
	newUser.set("password", password);
	newUser.set("defaultPassword", password);
	if (email)
		newUser.set("email", email);
	newUser.set("identity", userIdentity);

	// additional info
	if (userIdentity == 2 || userIdentity == 3) {		// Teacher
		var schoolId = request.params.schoolId;

		if (identity <= 3) {
			// check same school
			var userSchool = user.get("school");
			if (userSchool.id != schoolId){
				response.error("No permission");
				return;
			}
		}

		var querySchool = new Parse.Query("School");
		querySchool.equalTo("objectId", schoolId);
		querySchool.first({
			success: function(school) {
				newUser.set("school", school);

				var abbr = school.get("abbreviation");
				newUser.set("username", abbr + "." + username);
				newUser.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			},
			error: function() {
				response.error("Unknown error");
			}
		});

	} else {
		response.error("Unauthorized");
	}
});

Parse.Cloud.define("Web_Modify_User", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

	// receive parameters
    var objectId = request.params.objectId;
    var realName = request.params.realName;
    var email = request.params.email;
    var banned = parseInt(request.params.banned);
    //var userIdentity =  parseInt(request.params.identity);

	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("objectId", objectId);
	queryUser.first({
		success: function(targetUser) {
			if (identity <= 3) {
				// check same school
				var userSchool = user.get("school");
				var targetUserSchool = targetUser.get("school");
				if (userSchool.id != targetUserSchool.id){
					response.error("No permission");
					return;
				}
			}
			var targetIdentity = targetUser.get("identity");
			if (targetIdentity >= identity) {
				response.error("No permission");
				return;
			} else {
				targetUser.set("realName", realName);
				targetUser.set("email", email);
				targetUser.set("banned", banned);
				Parse.Cloud.useMasterKey();
				targetUser.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			}
		},
		error: function(error) {
			response.error(error);
		}
	});
});


Parse.Cloud.define("Web_ResetDefaultPassword", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

	// receive parameters
    var objectId = request.params.objectId;

	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("objectId", objectId);
	queryUser.first({
		success: function(targetUser) {
			if (identity <= 3) {
				// check same school
				var userSchool = user.get("school");
				var targetUserSchool = targetUser.get("school");
				if (userSchool.id != targetUserSchool.id){
					response.error("No permission");
					return;
				}
			}

			var targetIdentity = targetUser.get("identity");
			if (targetIdentity >= identity) {
				response.error("No permission");
				return;
			} else {
				var password = generatePassword();
				targetUser.set("password", password);
				targetUser.set("defaultPassword", password);
				Parse.Cloud.useMasterKey();
				targetUser.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			}
		},
		error: function(error) {
			response.error(error);
		}
	});
});

Parse.Cloud.define("Web_Get_User", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

	// checking identity valid
	if (identity < 2 || identity > 4) {
        response.error(ERROR_PERMISSION);
		return;
	}

    var targetUserId = request.params.userId;

	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("objectId", targetUserId);
	queryUser.include("school");
	queryUser.first({
		success: function(targetUser) {
			if (identity <= 3) {
				// check same school
				var userSchool = user.get("school");
				var targetUserSchool = targetUser.get("school");
				if (userSchool.id != targetUserSchool.id){
					response.error("No permission");
					return;
				}
			}
			var school = targetUser.get("school");
			var schoolNode = school.toJSON();

			response.success("{\"success\":" + NO_ERROR +
							",\"currentTime\":\"" + currentTime + "\"" +
							",\"school\":" + JSON.stringify(schoolNode) +
							",\"targetUser\":" + JSON.stringify(targetUser.toJSON()) +
							",\"user\":" + PreprocessUser(user) +
							"}");
		},
		error: function() {
			response.error("Unknown error");
		}
	});
});

Parse.Cloud.define("Web_Add_Course", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var objectId = request.params.objectId;	// if exist, it is edit
    var schoolId = request.params.schoolId;
    var courseTitle = request.params.courseTitle;
    var courseIcon = request.params.courseIcon;
    var teacherId = request.params.teacherId;

	// checking identity valid
	if (identity < 3) {
        response.error("No permission");
		return;
	}

	if (identity <= 3) {
		// check same school
		var userSchool = user.get("school");
		if (userSchool.id != schoolId){
			response.error("No permission");
			return;
		}
	}

	// basic info of new course
	var CourseClass = Parse.Object.extend("Course");
	var course = new CourseClass();
	if (!objectId) {
		// new
		if (courseIcon == null) {
			response.error("Add New Project without Icon provided");
			return;
		} else {
			course.set("courseIcon", courseIcon);
		}
	} else {
		// edit
		course.id = objectId;
		if (courseIcon != null) {
			course.set("courseIcon", courseIcon);
		}
	}

	course.set("courseTitle", courseTitle);


	var School = Parse.Object.extend("School");
	var pointerSchool = new School();
	pointerSchool.id = schoolId;
	course.set("school", pointerSchool);

	// additional info
	if (teacherId || teacherId != "") {// Teacher not designed, save
		//var TeacherClass = Parse.Object.extend();
		//var course = new CourseClass();
		var queryUser = new Parse.Query(Parse.User);
		queryUser.get(teacherId, {
			success: function(teacher) {
				course.set("courseTeacher", teacher);

				course.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error(error);
					}
				});
			},
			error: function(error) {
				response.error(error);
			}
		});
	} else {
		course.unset("courseTeacher");
		course.save(null,{
			success: function(object) {
				response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								",\"user\":" + PreprocessUser(user) +
								"}");
			},
			error: function(error) {
				response.error(error);
			}
		});
	}
});

Parse.Cloud.define("Web_Add_School", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var abbreviation = request.params.abbreviation;
    var fullName = request.params.fullName;

	// checking identity valid
	if (identity < 4) {
        response.error("No permission");
		return;
	}

	// basic info of new user
    var SchoolClass = Parse.Object.extend("School");
    var school = new SchoolClass();
	school.set("abbreviation", abbreviation);
	school.set("fullName", fullName);

	// duplicated?
	var query = new Parse.Query("School");
	query.equalTo("abbreviation", abbreviation);
	query.find({
		success: function(rs) {
			if (rs.length > 0) {
				response.error("Duplicate abbreviation existed");
			} else {
				school.save(null,{
				success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										",\"user\":" + PreprocessUser(user) +
										"}");
					},
					error: function(error) {
						response.error("Duplicate name existed");
					}
				});
			}
		},
		error: function() {
			response.error("Unknown error");
		}
	});
});



Parse.Cloud.define("Web_Modify_School", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

	var objectId = request.params.objectId;	// if exist, it is edit
    var fullName = request.params.fullName;

	// checking identity valid
	if (identity < 4) {
        response.error("No permission");
		return;
	}

	var query = new Parse.Query("School");
	query.equalTo("objectId", objectId);
	query.first({
		success: function(school) {
			school.set("fullName", fullName);
			school.save(null,{
				success: function(object) {
					response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"user\":" + PreprocessUser(user) +
									"}");
				},
				error: function(error) {
					response.error(error);
				}
			});
		},
		error: function() {
			response.error("Unknown error");
		}
	});
});

Parse.Cloud.define("Web_Delete_User", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var targetUserId = request.params.targetUserId;
	// checking identity valid
	if (identity < 3) {
        response.error("No permission");
		return;
	}

	var queryUser = new Parse.Query(Parse.User);
	queryUser.equalTo("objectId", targetUserId);
	queryUser.first({
		success: function(targetUser) {
			if (identity <= 3) {
				// check same school
				var userSchool = user.get("school");
				var targetUserSchool = targetUser.get("school");
				if (userSchool.id != targetUserSchool.id){
					response.error("No permission");
					return;
				}
			}
			if (targetUser == null) {
					response.error("user not exist");
					return;
			}
			Parse.Cloud.useMasterKey();
			targetUser.destroy({
				success: function(object) {
					response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"user\":" + PreprocessUser(user) +
									"}");
				},
				error: function(error) {
					response.error(error);
				}
			});
		},
		error: function(error) {
			response.error(error);
		}
	});
});

Parse.Cloud.define("Web_Delete_Course", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var courseId = request.params.courseId;
	// checking identity valid
	if (identity < 3) {
        response.error("No permission");
		return;
	}

	var query = new Parse.Query("Course");
	query.equalTo("objectId", courseId);
	query.first({
		success: function(course) {
			if (identity <= 3) {
				// check same school
				var userSchool = user.get("school");
				var targetCourseSchool = course.get("school");
				if (userSchool.id != targetCourseSchool.id){
					response.error("No permission");
					return;
				}
			}
			if (course == null) {
				response.error("course not exist");
				return;
			}
			Parse.Cloud.useMasterKey();
			course.destroy({
				success: function(object) {
					response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"user\":" + PreprocessUser(user) +
									"}");
				},
				error: function(error) {
					response.error(error);
				}
			});
		},
		error: function(error) {
			response.error(error);
		}
	});
});



Parse.Cloud.define("Web_Delete_School", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;
	var identity = user.get("identity");		// the user who trigger this function

    var schoolId = request.params.schoolId;
	// checking identity valid
	if (identity < 4) {
        response.error("No permission");
		return;
	}

	var query = new Parse.Query("School");
	query.equalTo("objectId", schoolId);
	query.first({
		success: function(school) {
			if (school == null) {
				response.error("school not exist");
				return;
			}
			Parse.Cloud.useMasterKey();
			school.destroy({
				success: function(object) {
					response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									",\"user\":" + PreprocessUser(user) +
									"}");
				},
				error: function(error) {
					response.error(error);
				}
			});
		},
		error: function(error) {
			response.error(error);
		}
	});
});


Parse.Cloud.define("Web_Delete_Project", function(request, response) {
	if (!ValidTime(request.params.timestamp)) {
		response.error(ERROR_INVALID_TIMESTAMP);
		return;
	}

    var currentTime =  Math.floor(new Date().getTime() / 1000);
    var user = request.user;

	// receive parameter
    var objectId = request.params.objectId;	// if exist, it is edit

	var identity = user.get("identity");
	if (identity <= 1) {
        response.error(ERROR_PERMISSION);
		return;
	}

	// project with same name?
	var queryExistProject = new Parse.Query("Project");
	queryExistProject.equalTo("objectId", objectId);
	queryExistProject.equalTo("isDeleted", 0);
	queryExistProject.include("course");
	queryExistProject.first({
		success: function(existedProject) {
			if (identity == 2) {
				// teacher, is this course own by this teacher?
				var course = existedProject.get("course");
				var teacher = course.get("courseTeacher");
				if (teacher == null) {
					response.error(ERROR_PERMISSION);
					return;
				} else {
					if (teacher.id == user.id) {
						existedProject.set("isDeleted", 1);
						existedProject.save(null,{
							success: function(object) {
								response.success("{\"success\":" + NO_ERROR +
										",\"currentTime\":\"" + currentTime + "\"" +
										"}");
							}
						});
					} else {
						response.error(ERROR_PERMISSION);
						return;
					}
				}
			} else if (identity == 3) {
				var course = existedProject.get("course");
				var school = course.get("school");
				var userSchool = user.get("school");
				if (school.id == userSchool.id) {
					existedProject.set("isDeleted", 1);
					existedProject.save(null,{
						success: function(object) {
							response.success("{\"success\":" + NO_ERROR +
									",\"currentTime\":\"" + currentTime + "\"" +
									"}");
						}
					});
				} else {
					response.error(ERROR_PERMISSION);
					return;
				}
			} else if (identity == 4) {
				existedProject.set("isDeleted", 1);
				existedProject.save(null,{
					success: function(object) {
						response.success("{\"success\":" + NO_ERROR +
								",\"currentTime\":\"" + currentTime + "\"" +
								"}");
					}
				});
			}
		},
		error: function() {
			response.error("queryProject error");
		}
	});
});
