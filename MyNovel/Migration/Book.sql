CREATE TABLE `book` (
  `bookID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `author` varchar(50) DEFAULT NULL COMMENT '¬O§_§¹µ²',
  `chapterCnt` int(10) unsigned DEFAULT 0,
  `url` varchar(255) DEFAULT NULL,
  `isEnd` tinyint(1) DEFAULT NULL,
  `updateTime` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`bookID`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;