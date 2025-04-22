CREATE TABLE `chapter` (
  `bookID` int(10) unsigned NOT NULL,
  `chapterID` int(10) unsigned NOT NULL,
  `title` varchar(255) NOT NULL,
  `content` longtext DEFAULT NULL,
  `insertTime` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`bookID`,`chapterID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;