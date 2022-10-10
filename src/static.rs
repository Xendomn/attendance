use actix_files as fs;

const ONEDRIVE_PATH: &'static str = "/mnt/c/Users/xendo/OneDrive/图片";
//const ONEDRIVE_PATH: &'static str = "C:/Users/xendo/OneDrive/图片";

const TELEGRAM_PATH: &'static str = "/home/daniel/Downloads/Telegram_Desktop";
// const TELEGRAM_PATH: &'static str = "E:/Downloads/Telegram_Desktop";

pub fn onedrive() -> fs::Files {
    fs::Files::new("/onedrive", ONEDRIVE_PATH).show_files_listing()
}

pub fn telegram() -> fs::Files {
    fs::Files::new("/telegram", TELEGRAM_PATH).show_files_listing()
}
