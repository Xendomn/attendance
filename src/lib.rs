pub mod glob;
pub mod r#static;
pub mod user;

#[macro_use]
extern crate rbatis;
use log;
use once_cell::sync::Lazy;
use rbatis::rbatis::Rbatis;

static RB: Lazy<Rbatis> = Lazy::new(|| Rbatis::new());

pub async fn init() {
    // init mysql db
    let res = RB
        // .link("postgres://postgres:root@localhost:5432/person")
        .link("mysql://root:root@localhost:3306/person")
        .await;

    match res {
        Ok(_) => {
            log::info!("Init database successful!");
        }
        Err(err) => {
            eprintln!("Error: {}", err);
            std::process::exit(1);
        }
    }
}
